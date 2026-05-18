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
    /// Begins a log scope with the given <paramref name="endpointScope" /> if no endpoint scope is already active.
    /// <para>
    /// When called inside a message-processing pipeline (where the slot scope already pushes endpoint metadata),
    /// this method returns a no-op disposable to avoid redundant scope enrichment.
    /// When called outside the pipeline (e.g., from Transactional Session or a hosted service),
    /// it pushes the endpoint scope onto the MEL scope stack.
    /// </para>
    /// </summary>
    public static IDisposable BeginEndpointScope(this ILogger logger, EndpointLoggingScope endpointScope)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(endpointScope);

        if (LogManager.TryGetCurrentEndpointScopeState(out _))
        {
            return NullScope.Instance;
        }

        return logger.BeginScope(endpointScope) ?? NullScope.Instance;
    }
}