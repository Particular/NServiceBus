#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Particular.Obsoletes;

/// <summary>
/// Represents an endpoint in the start-up phase where the container is externally managed.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public interface IStartableEndpointWithExternallyManagedContainer
{
    /// <summary>
    /// Starts the endpoint and returns a reference to it.
    /// </summary>
    /// <param name="builder">The <see cref="IServiceProvider"/> instance used to resolve dependencies.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A reference to the endpoint.</returns>
    [ObsoleteMetadata(
        Message = "Starting an endpoint manually is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
    [Obsolete("Starting an endpoint manually is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle including the start of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    Task<IEndpointInstance> Start(IServiceProvider builder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Access to the singleton IMessageSession to be registered in dependency injection container.
    /// Note: Lazily resolved since it's only valid for use once the endpoint has started.
    /// </summary>
    [ObsoleteMetadata(
        Message = "The message session is automatically registered in the service collection and no longer needs to be manually registered.",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12")]
    [Obsolete("The message session is automatically registered in the service collection and no longer needs to be manually registered.. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    Lazy<IMessageSession> MessageSession { get; }
}