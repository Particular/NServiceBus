#nullable enable

namespace NServiceBus.Logging;

using System;
using System.Collections.Concurrent;
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
    /// <summary>
    /// Used to inject an instance of <see cref="ILoggerFactory" /> into <see cref="LogManager" />.
    /// </summary>
    public static T Use<T>() where T : LoggingFactoryDefinition, new()
    {
        var loggingDefinition = new T();

        defaultLoggerFactory = new Lazy<ILoggerFactory>(loggingDefinition.GetLoggingFactory);

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
        return new SlotAwareLogger(type.FullName!);
    }

    /// <summary>
    /// Construct a <see cref="ILog" /> for <paramref name="name" />.
    /// </summary>
    public static ILog GetLogger(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new SlotAwareLogger(name);
    }

    internal static void RegisterSlotFactory(object slot, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(slot);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        slotLoggerFactories[new SlotKey(slot)] = loggerFactory;
    }

    internal static IDisposable BeginSlotScope(object slot)
    {
        ArgumentNullException.ThrowIfNull(slot);

        return new SlotScope(slot);
    }

    static ILoggerFactory GetLoggerFactoryForCurrentSlot()
    {
        var slot = currentSlot.Value;
        if (slot is not null && slotLoggerFactories.TryGetValue(new SlotKey(slot), out var loggerFactory))
        {
            return loggerFactory;
        }

        return defaultLoggerFactory.Value;
    }

    sealed class SlotAwareLogger(string name) : ILog
    {
        public bool IsDebugEnabled => GetLogger().IsDebugEnabled;
        public bool IsInfoEnabled => GetLogger().IsInfoEnabled;
        public bool IsWarnEnabled => GetLogger().IsWarnEnabled;
        public bool IsErrorEnabled => GetLogger().IsErrorEnabled;
        public bool IsFatalEnabled => GetLogger().IsFatalEnabled;

        public void Debug(string? message) => GetLogger().Debug(message);
        public void Debug(string? message, Exception? exception) => GetLogger().Debug(message, exception);
        public void DebugFormat(string format, params object?[] args) => GetLogger().DebugFormat(format, args);
        public void Info(string? message) => GetLogger().Info(message);
        public void Info(string? message, Exception? exception) => GetLogger().Info(message, exception);
        public void InfoFormat(string format, params object?[] args) => GetLogger().InfoFormat(format, args);
        public void Warn(string? message) => GetLogger().Warn(message);
        public void Warn(string? message, Exception? exception) => GetLogger().Warn(message, exception);
        public void WarnFormat(string format, params object?[] args) => GetLogger().WarnFormat(format, args);
        public void Error(string? message) => GetLogger().Error(message);
        public void Error(string? message, Exception? exception) => GetLogger().Error(message, exception);
        public void ErrorFormat(string format, params object?[] args) => GetLogger().ErrorFormat(format, args);
        public void Fatal(string? message) => GetLogger().Fatal(message);
        public void Fatal(string? message, Exception? exception) => GetLogger().Fatal(message, exception);
        public void FatalFormat(string format, params object?[] args) => GetLogger().FatalFormat(format, args);

        ILog GetLogger() => GetLoggerFactoryForCurrentSlot().GetLogger(name);
    }

    sealed class SlotScope : IDisposable
    {
        public SlotScope(object slot)
        {
            previousSlot = currentSlot.Value;
            currentSlot.Value = slot;
        }

        public void Dispose() => currentSlot.Value = previousSlot;

        readonly object? previousSlot;
    }

    readonly struct SlotKey(object value) : IEquatable<SlotKey>
    {
        public bool Equals(SlotKey other) => Equals(Value, other.Value);

        public override bool Equals(object? obj) => obj is SlotKey other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        readonly object Value = value;
    }

    static Lazy<ILoggerFactory> defaultLoggerFactory = new(new DefaultFactory().GetLoggingFactory);
    static readonly AsyncLocal<object?> currentSlot = new();
    static readonly ConcurrentDictionary<SlotKey, ILoggerFactory> slotLoggerFactories = new();
}