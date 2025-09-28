#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents a multi-endpoint host in the start-up phase where the container is externally managed.
/// </summary>
public interface IStartableMultiEndpointWithExternallyManagedContainer
{
    /// <summary>
    /// Starts all configured endpoints using the provided <see cref="IServiceProvider"/>.
    /// </summary>
    Task<IMultiEndpointInstance> Start(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
