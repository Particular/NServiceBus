#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents multiple running endpoints that share a single service collection.
/// </summary>
public interface IMultiEndpointInstance : IAsyncDisposable
{
    /// <summary>
    /// Gets metadata about all running endpoints.
    /// </summary>
    IReadOnlyCollection<EndpointInstanceInfo> Endpoints { get; }

    /// <summary>
    /// Gets the running endpoint instance associated with the provided endpoint name.
    /// </summary>
    IEndpointInstance GetByEndpointName(string endpointName);

    /// <summary>
    /// Gets the running endpoint instance associated with the provided service key.
    /// </summary>
    IEndpointInstance GetByKey(string serviceKey);

    /// <summary>
    /// Stops all running endpoints.
    /// </summary>
    Task Stop(CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes a running endpoint instance managed by <see cref="IMultiEndpointInstance"/>.
    /// </summary>
    public sealed class EndpointInstanceInfo
    {
        internal EndpointInstanceInfo(string endpointName, string serviceKey, IEndpointInstance instance)
        {
            EndpointName = endpointName;
            ServiceKey = serviceKey;
            Instance = instance;
        }

        /// <summary>
        /// The endpoint name.
        /// </summary>
        public string EndpointName { get; }

        /// <summary>
        /// The service key associated with the endpoint registrations.
        /// </summary>
        public string ServiceKey { get; }

        /// <summary>
        /// The running endpoint instance.
        /// </summary>
        public IEndpointInstance Instance { get; }
    }
}
