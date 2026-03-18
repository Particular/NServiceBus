#nullable enable

namespace NServiceBus;

using System;
using Logging;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using MicrosoftLogger = Microsoft.Extensions.Logging.ILogger;

sealed class MicrosoftLoggerFactoryAdapter(MicrosoftLoggerFactory loggerFactory) : ILoggerFactory
    , LogManager.ISlotScopedLoggerFactory
{
    public const string ScopeLoggerName = "NServiceBus.Logging.Scope";

    readonly MicrosoftLogger scopeLogger = loggerFactory.CreateLogger(ScopeLoggerName);

    public ILog GetLogger(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(type.FullName!));
    }

    public ILog GetLogger(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MicrosoftLoggerAdapter(loggerFactory.CreateLogger(name));
    }

    public IDisposable BeginScope(LogScopeState scopeState) => scopeLogger.BeginScope(scopeState) ?? NullScope.Instance;
}