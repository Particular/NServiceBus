#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

/// <summary>
/// Responsible for the creation of <see cref="ILog" /> instances and used as an extension point to redirect log events to
/// an external library.
/// </summary>
/// <remarks>
/// The default logging will be to the console and a rolling log file.
/// </remarks>
public static class LogManager
{
    internal interface ISlotScopedLoggerFactory
    {
        IDisposable BeginScope(LogScopeState scopeState);
    }

    /// <summary>
    /// Used to inject an instance of <see cref="ILoggerFactory" /> into <see cref="LogManager" />.
    /// </summary>
    public static T Use<T>() where T : LoggingFactoryDefinition, new()
    {
        var loggingDefinition = new T();

        defaultLoggerFactory = new Lazy<ILoggerFactory>(loggingDefinition.GetLoggingFactory);
        _ = Interlocked.Increment(ref defaultLoggerFactoryVersion);

        return loggingDefinition;
    }

    /// <summary>
    /// An instance of <see cref="ILoggerFactory" /> that will be used to construct <see cref="ILog" />s for static fields.
    /// </summary>
    /// <remarks>
    /// Replace this instance at application startup to redirect log events to the custom logging library.
    /// </remarks>
    public static void UseFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        defaultLoggerFactory = new Lazy<ILoggerFactory>(() => loggerFactory);
        _ = Interlocked.Increment(ref defaultLoggerFactoryVersion);
    }

    /// <summary>
    /// Construct a <see cref="ILog" /> using <typeparamref name="T" /> as the name.
    /// </summary>
    public static ILog GetLogger<T>() => GetLogger(typeof(T));

    /// <summary>
    /// Construct a <see cref="ILog" /> using <paramref name="type" /> as the name.
    /// </summary>
    public static ILog GetLogger(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return GetLogger(type.FullName!);
    }

    /// <summary>
    /// Construct a <see cref="ILog" /> for <paramref name="name" />.
    /// </summary>
    public static ILog GetLogger(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return loggers.GetOrAdd(name, static loggerName => new SlotAwareLogger(loggerName));
    }

    internal static void RegisterSlotFactory(object slot, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var slotKey = new SlotKey(slot);
        slotLoggerFactories[slotKey] = loggerFactory;
        var slotContext = GetOrAddSlotContext(slotKey);

        using var _ = new SlotScope(slotContext, activateExternalScope: true);
        foreach (var logger in loggers.Values)
        {
            logger.Flush(slotKey, loggerFactory);
        }
    }

    internal static IDisposable BeginSlotScope(object slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var slotKey = new SlotKey(slot);
        var slotContext = GetOrAddSlotContext(slotKey);
        return new SlotScope(slotContext, activateExternalScope: true);
    }

    internal static bool TryGetCurrentEndpointScopeState([NotNullWhen(true)] out LogScopeState? scopeState)
    {
        if (currentSlot.Value is null)
        {
            scopeState = null;
            return false;
        }

        scopeState = currentSlot.Value.ScopeState;
        return true;
    }

    static SlotContext GetOrAddSlotContext(SlotKey slotKey) =>
        slotContexts.GetOrAdd(slotKey, static key => new SlotContext(key.Value, CreateScopeState(key.Value)));

    static LogScopeState CreateScopeState(object slot) =>
        slot is LogSlot logSlot ? logSlot.ScopeState : new LogSCopeStates(slot, endpointIdentifier: null);

    sealed class SlotAwareLogger(string name) : ILog
    {
        public bool IsDebugEnabled => IsEnabled(static l => l.IsDebugEnabled);
        public bool IsInfoEnabled => IsEnabled(static l => l.IsInfoEnabled);
        public bool IsWarnEnabled => IsEnabled(static l => l.IsWarnEnabled);
        public bool IsErrorEnabled => IsEnabled(static l => l.IsErrorEnabled);
        public bool IsFatalEnabled => IsEnabled(static l => l.IsFatalEnabled);

        public void Debug(string? message) => Write(LogLevel.Debug, message,
            static (logger, payload) => logger.Debug(payload));

        public void Debug(string? message, Exception? exception) => Write(LogLevel.Debug, message, exception,
            static (logger, payload, ex) => logger.Debug(payload, ex));

        public void DebugFormat(string format, params object?[] args) => Write(LogLevel.Debug, format, args,
            static (logger, payload, payloadArgs) => logger.DebugFormat(payload, payloadArgs));

        public void Info(string? message) => Write(LogLevel.Info, message,
            static (logger, payload) => logger.Info(payload));

        public void Info(string? message, Exception? exception) => Write(LogLevel.Info, message, exception,
            static (logger, payload, ex) => logger.Info(payload, ex));

        public void InfoFormat(string format, params object?[] args) => Write(LogLevel.Info, format, args,
            static (logger, payload, payloadArgs) => logger.InfoFormat(payload, payloadArgs));

        public void Warn(string? message) => Write(LogLevel.Warn, message,
            static (logger, payload) => logger.Warn(payload));

        public void Warn(string? message, Exception? exception) => Write(LogLevel.Warn, message, exception,
            static (logger, payload, ex) => logger.Warn(payload, ex));

        public void WarnFormat(string format, params object?[] args) => Write(LogLevel.Warn, format, args,
            static (logger, payload, payloadArgs) => logger.WarnFormat(payload, payloadArgs));

        public void Error(string? message) => Write(LogLevel.Error, message,
            static (logger, payload) => logger.Error(payload));

        public void Error(string? message, Exception? exception) => Write(LogLevel.Error, message, exception,
            static (logger, payload, ex) => logger.Error(payload, ex));

        public void ErrorFormat(string format, params object?[] args) => Write(LogLevel.Error, format, args,
            static (logger, payload, payloadArgs) => logger.ErrorFormat(payload, payloadArgs));

        public void Fatal(string? message) => Write(LogLevel.Fatal, message,
            static (logger, payload) => logger.Fatal(payload));

        public void Fatal(string? message, Exception? exception) => Write(LogLevel.Fatal, message, exception,
            static (logger, payload, ex) => logger.Fatal(payload, ex));

        public void FatalFormat(string format, params object?[] args) => Write(LogLevel.Fatal, format, args,
            static (logger, payload, payloadArgs) => logger.FatalFormat(payload, payloadArgs));

        public void Flush(SlotKey slotKey, ILoggerFactory loggerFactory)
        {
            if (!deferredLogsBySlot.TryGetValue(slotKey, out var deferredLogs))
            {
                return;
            }

            var logger = slotLoggers.GetOrAdd(slotKey, static (_, state) => state.loggerFactory.GetLogger(state.name), (loggerFactory, name));
            deferredLogs.FlushTo(logger);
        }

        bool IsEnabled(Func<ILog, bool> isEnabled)
        {
            if (TryGetLogger(out var logger))
            {
                return isEnabled(logger);
            }

            return TryGetCurrentSlotContext(out _) || isEnabled(GetDefaultLogger());
        }

        void Write(LogLevel level, string? message, Action<ILog, string?> writeAction)
        {
            if (TryGetLogger(out var logger))
            {
                writeAction(logger, message);
                return;
            }

            if (TryGetCurrentSlotContext(out var slotContext))
            {
                var deferredLogs = deferredLogsBySlot.GetOrAdd(slotContext.Key, _ => new DeferredLogs());
                deferredLogs.DeferredMessageLogs.Enqueue((level, message));
                return;
            }

            writeAction(GetDefaultLogger(), message);
        }

        void Write(LogLevel level, string? message, Exception? exception, Action<ILog, string?, Exception?> writeAction)
        {
            if (TryGetLogger(out var logger))
            {
                writeAction(logger, message, exception);
                return;
            }

            if (TryGetCurrentSlotContext(out var slotContext))
            {
                var deferredLogs = deferredLogsBySlot.GetOrAdd(slotContext.Key, _ => new DeferredLogs());
                deferredLogs.DeferredExceptionLogs.Enqueue((level, message, exception));
                return;
            }

            writeAction(GetDefaultLogger(), message, exception);
        }

        void Write(LogLevel level, string format, object?[] args, Action<ILog, string, object?[]> writeAction)
        {
            if (TryGetLogger(out var logger))
            {
                writeAction(logger, format, args);
                return;
            }

            if (TryGetCurrentSlotContext(out var slotContext))
            {
                var deferredLogs = deferredLogsBySlot.GetOrAdd(slotContext.Key, _ => new DeferredLogs());
                deferredLogs.DeferredFormatLogs.Enqueue((level, format, args));
                return;
            }

            writeAction(GetDefaultLogger(), format, args);
        }

        ILog GetDefaultLogger()
        {
            var currentFactoryVersion = Volatile.Read(ref defaultLoggerFactoryVersion);
            if (defaultLogger is not null && defaultLoggerFactoryVersionSnapshot == currentFactoryVersion)
            {
                return defaultLogger;
            }

            var createdLogger = defaultLoggerFactory.Value.GetLogger(name);
            defaultLogger = createdLogger;
            defaultLoggerFactoryVersionSnapshot = currentFactoryVersion;
            return createdLogger;
        }

        bool TryGetLogger(out ILog logger)
        {
            var slotContext = currentSlot.Value;
            if (slotContext is null)
            {
                logger = null!;
                return false;
            }

            if (ReferenceEquals(cachedSlotContext, slotContext) && cachedSlotLogger is not null)
            {
                logger = cachedSlotLogger;
                return true;
            }

            if (slotLoggerFactories.TryGetValue(slotContext.Key, out var loggerFactory))
            {
                logger = slotLoggers.GetOrAdd(slotContext.Key, static (_, state) => state.loggerFactory.GetLogger(state.name), (name, loggerFactory));
                cachedSlotContext = slotContext;
                cachedSlotLogger = logger;
                return true;
            }

            logger = null!;
            return false;
        }

        static bool TryGetCurrentSlotContext([NotNullWhen(true)] out SlotContext? slotContext)
        {
            slotContext = currentSlot.Value;
            return slotContext is not null;
        }

        ILog? defaultLogger;
        int defaultLoggerFactoryVersionSnapshot = -1;
        SlotContext? cachedSlotContext;
        ILog? cachedSlotLogger;
        readonly ConcurrentDictionary<SlotKey, DeferredLogs> deferredLogsBySlot = new();
        readonly ConcurrentDictionary<SlotKey, ILog> slotLoggers = new();
    }

    sealed class DeferredLogs
    {
        public readonly ConcurrentQueue<(LogLevel level, string? message)> DeferredMessageLogs = new();
        public readonly ConcurrentQueue<(LogLevel level, string? message, Exception? exception)> DeferredExceptionLogs = new();
        public readonly ConcurrentQueue<(LogLevel level, string format, object?[] args)> DeferredFormatLogs = new();

        public void FlushTo(ILog logger)
        {
            while (DeferredMessageLogs.TryDequeue(out var messageLog))
            {
                switch (messageLog.level)
                {
                    case LogLevel.Debug:
                        logger.Debug(messageLog.message);
                        break;
                    case LogLevel.Info:
                        logger.Info(messageLog.message);
                        break;
                    case LogLevel.Warn:
                        logger.Warn(messageLog.message);
                        break;
                    case LogLevel.Error:
                        logger.Error(messageLog.message);
                        break;
                    case LogLevel.Fatal:
                        logger.Fatal(messageLog.message);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported log level '{messageLog.level}'.");
                }
            }

            while (DeferredExceptionLogs.TryDequeue(out var exceptionLog))
            {
                switch (exceptionLog.level)
                {
                    case LogLevel.Debug:
                        logger.Debug(exceptionLog.message, exceptionLog.exception);
                        break;
                    case LogLevel.Info:
                        logger.Info(exceptionLog.message, exceptionLog.exception);
                        break;
                    case LogLevel.Warn:
                        logger.Warn(exceptionLog.message, exceptionLog.exception);
                        break;
                    case LogLevel.Error:
                        logger.Error(exceptionLog.message, exceptionLog.exception);
                        break;
                    case LogLevel.Fatal:
                        logger.Fatal(exceptionLog.message, exceptionLog.exception);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported log level '{exceptionLog.level}'.");
                }
            }

            while (DeferredFormatLogs.TryDequeue(out var formatLog))
            {
                switch (formatLog.level)
                {
                    case LogLevel.Debug:
                        logger.DebugFormat(formatLog.format, formatLog.args);
                        break;
                    case LogLevel.Info:
                        logger.InfoFormat(formatLog.format, formatLog.args);
                        break;
                    case LogLevel.Warn:
                        logger.WarnFormat(formatLog.format, formatLog.args);
                        break;
                    case LogLevel.Error:
                        logger.ErrorFormat(formatLog.format, formatLog.args);
                        break;
                    case LogLevel.Fatal:
                        logger.FatalFormat(formatLog.format, formatLog.args);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported log level '{formatLog.level}'.");
                }
            }
        }
    }

    sealed class SlotScope : IDisposable
    {
        public SlotScope(SlotContext slot, bool activateExternalScope)
        {
            previousSlot = currentSlot.Value;
            currentSlot.Value = slot;

            if (activateExternalScope && slotLoggerFactories.TryGetValue(slot.Key, out var loggerFactory) && loggerFactory is ISlotScopedLoggerFactory slotScopedLoggerFactory)
            {
                activeScope = slotScopedLoggerFactory.BeginScope(slot.ScopeState);
            }
        }

        public void Dispose()
        {
            activeScope?.Dispose();
            currentSlot.Value = previousSlot;
        }

        readonly SlotContext? previousSlot;
        readonly IDisposable? activeScope;
    }

    sealed class SlotContext(object identifier, LogScopeState scopeState)
    {
        public SlotKey Key { get; } = new(identifier);
        public LogScopeState ScopeState { get; } = scopeState;
    }

    readonly struct SlotKey(object value) : IEquatable<SlotKey>
    {
        public object Value { get; } = value;

        public bool Equals(SlotKey other) => Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is SlotKey other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

    }

    static Lazy<ILoggerFactory> defaultLoggerFactory = new(new DefaultFactory().GetLoggingFactory);
    static readonly AsyncLocal<SlotContext?> currentSlot = new();
    static readonly ConcurrentDictionary<string, SlotAwareLogger> loggers = new(StringComparer.Ordinal);
    static readonly ConcurrentDictionary<SlotKey, SlotContext> slotContexts = new();
    static readonly ConcurrentDictionary<SlotKey, ILoggerFactory> slotLoggerFactories = new();
    static int defaultLoggerFactoryVersion;
}