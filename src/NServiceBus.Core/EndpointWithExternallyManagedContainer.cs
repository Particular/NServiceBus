namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;
using Particular.Obsoletes;

/// <summary>
/// Provides factory methods for creating endpoints instances with an externally managed container.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint using the EndpointWithExternallyManagedContainer.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint.",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint using the EndpointWithExternallyManagedContainer.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public static class EndpointWithExternallyManagedContainer
{
    /// <summary>
    /// Creates a new startable endpoint based on the provided configuration that uses an externally managed container.
    /// </summary>
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint using the EndpointWithExternallyManagedContainer.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint.",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint using the EndpointWithExternallyManagedContainer.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint.. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static IStartableEndpointWithExternallyManagedContainer Create(EndpointConfiguration configuration, IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return EndpointExternallyManaged.Create(configuration, serviceCollection);
    }
}