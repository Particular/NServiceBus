#nullable enable

namespace NServiceBus;

using System;

interface IEndpointCreationStrategy
{
    LogSlot EndpointLogSlot { get; }
    StartableEndpoint CreateStartableEndpoint(IServiceProvider serviceProvider);
}

sealed class InternalContainerEndpointCreationStrategy(EndpointCreator endpointCreator, IAsyncDisposable? serviceProviderLease = null) : IEndpointCreationStrategy
{
    public LogSlot EndpointLogSlot => endpointCreator.EndpointLogSlot;

    public StartableEndpoint CreateStartableEndpoint(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var lease = serviceProviderLease ?? serviceProvider as IAsyncDisposable ?? NoOpAsyncDisposable.Instance;
        return endpointCreator.CreateStartableEndpoint(serviceProvider, "internal", lease);
    }
}

sealed class ExternalContainerEndpointCreationStrategy(EndpointCreator endpointCreator) : IEndpointCreationStrategy
{
    public LogSlot EndpointLogSlot => endpointCreator.EndpointLogSlot;

    public StartableEndpoint CreateStartableEndpoint(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return endpointCreator.CreateStartableEndpoint(serviceProvider, "external", NoOpAsyncDisposable.Instance);
    }
}