#nullable enable

namespace NServiceBus.Logging;

using System;
using Microsoft.Extensions.Logging;

/// <summary>
/// Extension methods for <see cref="ILogger" /> that provide endpoint-aware scope enrichment.
/// </summary>
public static class EndpointLoggingScopeExtensions
{
    /// <summary>
    /// Begins a log scope that enriches log output with endpoint name and identifier.
    /// <para>
    /// When called within the endpoint's message-processing pipeline, this method detects
    /// that the endpoint context is already active and returns a no-op scope, avoiding
    /// duplicate scope entries in the log output.
    /// </para>
    /// <para>
    /// When called outside the pipeline (e.g., from a hosted service, background task,
    /// or Transactional Session), this method activates the endpoint's logging context so
    /// that <see cref="ILogger" /> and <see cref="ILog" /> output includes the endpoint
    /// name and identifier, just as if the log were written during message processing.
    /// </para>
    /// <para>
    /// Resolve <see cref="EndpointLoggingScope" /> from dependency injection to obtain
    /// the current endpoint's scope information.
    /// </para>
    /// </summary>
    public static IDisposable BeginEndpointScope(this ILogger logger, EndpointLoggingScope endpointScope)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(endpointScope);

        if (endpointScope.Slot is not null)
        {
            return LogManager.BeginSlotScope(endpointScope.Slot);
        }

        return logger.BeginScope(endpointScope) ?? NullScope.Instance;
    }
}