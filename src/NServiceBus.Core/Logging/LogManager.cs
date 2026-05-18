#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Particular.Obsoletes;
using MicrosoftILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MicrosoftLogLevel = Microsoft.Extensions.Logging.LogLevel;

/// <summary>
/// Responsible for the creation of <see cref="ILog" /> instances and used as an extension point to redirect log events to
/// an external library.
/// </summary>
/// <remarks>
/// The default logging will be to the console and a rolling log file.
/// </remarks>
public static class LogManager
{
    /// <summary>
    /// Used to inject an instance of <see cref="ILoggerFactory" /> into <see cref="LogManager" />.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Configure logging through Microsoft.Extensions.Logging instead",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12")]
    [Obsolete("Configure logging through Microsoft.Extensions.Logging instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
#pragma warning disable CS0618 // DefaultFactory and LoggingFactoryDefinition are deprecated; LogManager must reference them internally during the deprecation window
    public static T Use<T>() where T : LoggingFactoryDefinition, new()
    {
        var loggingDefinition = new T();
        defaultLoggerFactoryDefinition = loggingDefinition;

        defaultLoggerFactory = new Lazy<ILoggerFactory>(loggingDefinition.GetLoggingFactory);
        _ = Interlocked.Increment(ref defaultLoggerFactoryVersion);

        return loggingDefinition;
    }
#pragma warning restore CS0618

    /// <summary>
    /// An instance of <see cref="ILoggerFactory" /> that will be used to construct <see cref="ILog" />s for static fields.
    /// </summary>
    /// <remarks>
    /// Replace this instance at application startup to redirect log events to the custom logging library.
    /// </remarks>
    [ObsoleteMetadata(
        Message = "Configure logging through Microsoft.Extensions.Logging instead",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12")]
    [Obsolete("Configure logging through Microsoft.Extensions.Logging instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static void UseFactory(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        defaultLoggerFactory = new Lazy<ILoggerFactory>(() => loggerFactory);
        defaultLoggerFactoryDefinition = null;
        _ = Interlocked.Increment(ref defaultLoggerFactoryVersion);
    }

    internal static DefaultLoggingConfiguration? GetLoggingConfiguration()
    {
        if (IsExternalFactoryConfigured)
        {
            return null;
        }

#pragma warning disable CS0618 // DefaultFactory is deprecated; LogManager keeps compatibility wiring during deprecation window
        var factory = defaultLoggerFactoryDefinition as DefaultFactory;
        return new DefaultLoggingConfiguration(factory?.LoggingDirectory ?? Host.GetOutputDirectory(), factory?.LoggingLevel ?? LogLevel.Info);
#pragma warning restore CS0618
    }

    static ILoggerFactory? GetExternalFactoryIfAvailable() => IsExternalFactoryConfigured ? defaultLoggerFactory.Value : null;

    static bool IsExternalFactoryConfigured => defaultLoggerFactoryDefinition is null;

    internal static bool DefaultFactoryIsUnsupported => defaultLoggerFactory.Value is IUnsupportedDefaultFactoryLoggerFactory;

    /// <summary>
    /// Sets the ambient default factory for the current async context.
    /// Used by acceptance testing to route out-of-slot logs to the scenario's MEL factory
    /// without mutating process-global static state.
    /// </summary>
    internal static void SetAmbientDefaultFactory(MicrosoftILoggerFactory? factory) => ambientDefaultFactory.Value = factory;

    internal static MicrosoftILoggerFactory? GetAmbientDefaultFactory() => ambientDefaultFactory.Value;

    internal sealed class DefaultLoggingConfiguration(string loggingDirectory, LogLevel nsbLogLevel)
    {
        public string LoggingDirectory { get; } = loggingDirectory;
        public MicrosoftLogLevel MicrosoftLogLevel => ConvertLogLevel(nsbLogLevel);

        static MicrosoftLogLevel ConvertLogLevel(LogLevel level) =>
            level switch
            {
                LogLevel.Debug => MicrosoftLogLevel.Debug,
                LogLevel.Info => MicrosoftLogLevel.Information,
                LogLevel.Warn => MicrosoftLogLevel.Warning,
                LogLevel.Error => MicrosoftLogLevel.Error,
                LogLevel.Fatal => MicrosoftLogLevel.Critical,
                _ => MicrosoftLogLevel.Information
            };
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

    internal static ILoggerFactory Adapt(MicrosoftILoggerFactory microsoftLoggerFactory) =>
        GetExternalFactoryIfAvailable() is { } externalFactory
            ? new ExternalLoggerFactoryAdapter(externalFactory, microsoftLoggerFactory)
            : new MicrosoftLoggerFactoryAdapter(microsoftLoggerFactory);

    internal static void RegisterSlotFactory(object slot, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        var slotKey = new SlotKey(slot);
        slotLoggerFactories[slotKey] = loggerFactory;
        var slotContext = GetOrAddSlotContext(slotKey);

        using var _ = new SlotScope(slotContext);
        DrainScopedStartupLogs(slotKey, loggerFactory);
    }

    internal static void UnregisterSlot(object slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var slotKey = new SlotKey(slot);

        if (!slotLoggerFactories.ContainsKey(slotKey) && scopedStartupLogsBySlot.TryRemove(slotKey, out var pendingScopedLogs))
        {
            var drainLogger = GetAmbientDefaultFactory() is { } ambientFactory
                ? new MicrosoftLoggerAdapter(ambientFactory.CreateLogger(nameof(LogManager)))
                : FallbackLoggerFactory.Instance.Value.GetLogger(nameof(LogManager));

            while (pendingScopedLogs.TryDequeue(out var entry))
            {
                entry.WriteTo(drainLogger);
            }
        }

        slotContexts.TryRemove(slotKey, out _);
        slotLoggerFactories.TryRemove(slotKey, out _);
        scopedStartupLogsBySlot.TryRemove(slotKey, out _);

        foreach (var logger in loggers.Values)
        {
            logger.RemoveSlot(slotKey);
        }
    }

    internal static SlotScope BeginSlotScope(object slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        var slotKey = new SlotKey(slot);
        var slotContext = GetOrAddSlotContext(slotKey);
        return new SlotScope(slotContext);
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
        slot is LogSlot logSlot ? logSlot.ScopeState : new LogScopeStates(slot, endpointIdentifier: null);

    static void EnqueueScopedStartupLog(SlotKey slotKey, DeferredLogEntry entry)
    {
        var queue = scopedStartupLogsBySlot.GetOrAdd(slotKey, static _ => new ConcurrentQueue<DeferredLogEntry>());
        queue.Enqueue(entry);
    }

    static void DrainScopedStartupLogs(SlotKey slotKey, ILoggerFactory loggerFactory)
    {
        if (!scopedStartupLogsBySlot.TryRemove(slotKey, out var scopedLogs))
        {
            return;
        }

        var cachedLoggers = new Dictionary<string, ILog>(StringComparer.Ordinal);
        while (scopedLogs.TryDequeue(out var entry))
        {
            if (!cachedLoggers.TryGetValue(entry.LoggerName, out var logger))
            {
                logger = loggerFactory.GetLogger(entry.LoggerName);
                cachedLoggers.Add(entry.LoggerName, logger);
            }

            entry.WriteTo(logger);
        }
    }

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

        public void RemoveSlot(SlotKey slotKey)
        {
            slotLoggers.TryRemove(slotKey, out _);

            // Invalidate the instance-level cache if it was pointing at the removed slot
            // so the next TryGetLogger call falls through to slotLoggerFactories (which
            // is now empty for this slot) and eventually the default logger.
            if (Volatile.Read(ref cachedSlot)?.Context.Key.Equals(slotKey) == true)
            {
                Volatile.Write(ref cachedSlot, null);
            }
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
                WriteOrEnqueueScopedStartupLog(slotContext, DeferredLogEntry.Message(name, level, message));
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
                WriteOrEnqueueScopedStartupLog(slotContext, DeferredLogEntry.Exception(name, level, message, exception));
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
                WriteOrEnqueueScopedStartupLog(slotContext, DeferredLogEntry.Format(name, level, format, args));
                return;
            }

            writeAction(GetDefaultLogger(), format, args);
        }

        ILog GetDefaultLogger()
        {
            if (GetAmbientDefaultFactory() is { } ambientFactory)
            {
                return new MicrosoftLoggerAdapter(ambientFactory.CreateLogger(name));
            }

            var currentFactoryVersion = Volatile.Read(ref defaultLoggerFactoryVersion);
            if (defaultLoggerFactoryVersionSnapshot == currentFactoryVersion && defaultLogger is { } cached)
            {
                return cached;
            }

            var loggerFactory = defaultLoggerFactory.Value;
            var createdLogger = loggerFactory is IUnsupportedDefaultFactoryLoggerFactory
                ? FallbackLoggerFactory.Instance.Value.GetLogger(name)
                : loggerFactory.GetLogger(name);

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

            // Use a single volatile read so both fields (context + logger) are observed
            // atomically
            var cachedEntry = Volatile.Read(ref cachedSlot);
            if (cachedEntry is not null && ReferenceEquals(cachedEntry.Context, slotContext))
            {
                logger = cachedEntry.Logger;
                return true;
            }

            if (slotLoggerFactories.TryGetValue(slotContext.Key, out var loggerFactory))
            {
                var resolvedLogger = slotLoggers.GetOrAdd(slotContext.Key, static (_, state) => state.loggerFactory.GetLogger(state.name), (name, loggerFactory));

                logger = resolvedLogger;
                // Publish both context and logger together as one reference so concurrent
                // reads either see the old entry or the fully-initialised new one.
                Volatile.Write(ref cachedSlot, new CachedSlot(slotContext, resolvedLogger));
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

        static void WriteOrEnqueueScopedStartupLog(SlotContext slotContext, DeferredLogEntry entry)
        {
            if (slotContexts.ContainsKey(slotContext.Key))
            {
                EnqueueScopedStartupLog(slotContext.Key, entry);
                return;
            }

            if (GetAmbientDefaultFactory() is { } ambientFactory)
            {
                entry.WriteTo(new MicrosoftLoggerAdapter(ambientFactory.CreateLogger(entry.LoggerName)));
                return;
            }

            entry.WriteTo(FallbackLoggerFactory.Instance.Value.GetLogger(entry.LoggerName));
        }

        ILog? defaultLogger;
        volatile int defaultLoggerFactoryVersionSnapshot = -1;
        CachedSlot? cachedSlot;
        readonly ConcurrentDictionary<SlotKey, ILog> slotLoggers = new();

        // Bundles SlotContext and its resolved ILog into a single reference so Volatile.Read/Write
        // in TryGetLogger and RemoveSlot see either the old pair or the new pair
        sealed class CachedSlot(SlotContext context, ILog logger)
        {
            public SlotContext Context { get; } = context;
            public ILog Logger { get; } = logger;
        }
    }

    sealed class FallbackLoggerFactory : ILoggerFactory
    {
        public static readonly Lazy<ILoggerFactory> Instance = new(Create);

        readonly MicrosoftILoggerFactory melFactory;

        FallbackLoggerFactory(MicrosoftILoggerFactory melFactory) => this.melFactory = melFactory;

        ILog ILoggerFactory.GetLogger(string name) => new MicrosoftLoggerAdapter(melFactory.CreateLogger(name));

        ILog ILoggerFactory.GetLogger(Type type) => GetLogger(type.FullName ?? type.Name);

        static FallbackLoggerFactory Create()
        {
            var config = GetLoggingConfiguration();
            var directory = config?.LoggingDirectory ?? Host.GetOutputDirectory();
            var level = config?.MicrosoftLogLevel ?? MicrosoftLogLevel.Information;

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.Services.Configure<RollingLoggerProviderOptions>(o =>
                {
                    o.Directory = directory;
                    o.LogLevel = level;
                });
                builder.AddNServiceBusLoggingProviders(directory, level);
                builder.SetMinimumLevel(level);
            });

            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<MicrosoftILoggerFactory>();
            return new FallbackLoggerFactory(factory);
        }
    }

    readonly struct DeferredLogEntry(string loggerName, DeferredLogEntryKind kind, LogLevel level, string? text, Exception? exception, object?[]? args)
    {
        public string LoggerName { get; } = loggerName;

        public static DeferredLogEntry Message(string loggerName, LogLevel level, string? message) =>
            new(loggerName, DeferredLogEntryKind.Message, level, message, exception: null, args: null);

        public static DeferredLogEntry Exception(string loggerName, LogLevel level, string? message, Exception? exception) =>
            new(loggerName, DeferredLogEntryKind.Exception, level, message, exception, args: null);

        public static DeferredLogEntry Format(string loggerName, LogLevel level, string format, object?[] args) =>
            new(loggerName, DeferredLogEntryKind.Format, level, format, exception: null, args);

        public void WriteTo(ILog logger)
        {
            switch (kind)
            {
                case DeferredLogEntryKind.Message:
                    WriteMessage(logger, level, text);
                    return;
                case DeferredLogEntryKind.Exception:
                    WriteException(logger, level, text, exception);
                    return;
                case DeferredLogEntryKind.Format:
                    WriteFormat(logger, level, text!, args!);
                    return;
                default:
                    throw new InvalidOperationException($"Unsupported deferred log entry kind '{kind}'.");
            }
        }

        static void WriteMessage(ILog logger, LogLevel level, string? message)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    logger.Debug(message);
                    break;
                case LogLevel.Info:
                    logger.Info(message);
                    break;
                case LogLevel.Warn:
                    logger.Warn(message);
                    break;
                case LogLevel.Error:
                    logger.Error(message);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal(message);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported log level '{level}'.");
            }
        }

        static void WriteException(ILog logger, LogLevel level, string? message, Exception? exception)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    logger.Debug(message, exception);
                    break;
                case LogLevel.Info:
                    logger.Info(message, exception);
                    break;
                case LogLevel.Warn:
                    logger.Warn(message, exception);
                    break;
                case LogLevel.Error:
                    logger.Error(message, exception);
                    break;
                case LogLevel.Fatal:
                    logger.Fatal(message, exception);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported log level '{level}'.");
            }
        }

        static void WriteFormat(ILog logger, LogLevel level, string format, object?[] args)
        {
            switch (level)
            {
                case LogLevel.Debug:
                    logger.DebugFormat(format, args);
                    break;
                case LogLevel.Info:
                    logger.InfoFormat(format, args);
                    break;
                case LogLevel.Warn:
                    logger.WarnFormat(format, args);
                    break;
                case LogLevel.Error:
                    logger.ErrorFormat(format, args);
                    break;
                case LogLevel.Fatal:
                    logger.FatalFormat(format, args);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported log level '{level}'.");
            }
        }
    }

    enum DeferredLogEntryKind
    {
        Message,
        Exception,
        Format
    }

    internal readonly struct SlotScope : IDisposable
    {
        internal SlotScope(SlotContext slot)
        {
            previousSlot = currentSlot.Value;
            currentSlot.Value = slot;

            // Skip pushing a duplicate MEL scope when already inside the same slot.
            // The endpoint's slot scope is activated at multiple points in the call stack:
            // endpoint startup, message pump receive, and MessageSession operations.
            // The AsyncLocal routing correctly nests via previousSlot/restore, but MEL
            // BeginScope is additive — without this check the same endpoint metadata
            // would appear multiple times in scope state.
            var alreadyInSameSlot = previousSlot is not null && previousSlot.Key.Equals(slot.Key);

            if (!alreadyInSameSlot && slotLoggerFactories.TryGetValue(slot.Key, out var loggerFactory) && loggerFactory is ISlotScopedLoggerFactory slotScopedLoggerFactory)
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

    internal sealed class SlotContext(object identifier, LogScopeState scopeState)
    {
        public SlotKey Key { get; } = new(identifier);
        public LogScopeState ScopeState { get; } = scopeState;
    }

    internal readonly struct SlotKey(object value) : IEquatable<SlotKey>
    {
        public object Value { get; } = value;

        public bool Equals(SlotKey other) => Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is SlotKey other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
    }

#pragma warning disable CS0618 // DefaultFactory and LoggingFactoryDefinition are deprecated; LogManager must reference them internally during the deprecation window
    static Lazy<ILoggerFactory> defaultLoggerFactory = new(new DefaultFactory().GetLoggingFactory);
    static LoggingFactoryDefinition? defaultLoggerFactoryDefinition = new DefaultFactory();
#pragma warning restore CS0618
    static readonly AsyncLocal<SlotContext?> currentSlot = new();
    static readonly AsyncLocal<MicrosoftILoggerFactory?> ambientDefaultFactory = new();
    static readonly ConcurrentDictionary<string, SlotAwareLogger> loggers = new(StringComparer.Ordinal);
    static readonly ConcurrentDictionary<SlotKey, SlotContext> slotContexts = new();
    static readonly ConcurrentDictionary<SlotKey, ILoggerFactory> slotLoggerFactories = new();
    static readonly ConcurrentDictionary<SlotKey, ConcurrentQueue<DeferredLogEntry>> scopedStartupLogsBySlot = new();
    static int defaultLoggerFactoryVersion;
}