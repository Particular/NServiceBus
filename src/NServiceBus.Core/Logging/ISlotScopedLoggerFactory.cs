#nullable enable

namespace NServiceBus;

using System;

interface ISlotScopedLoggerFactory
{
    IDisposable BeginScope(LogScopeState scopeState);
}