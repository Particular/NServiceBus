#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete -- In the next major version this type can remove the interface implementation but quite likely still has reasons to exist
class ExternallyManagedContainerHost(EndpointCreator endpointCreator) : IStartableEndpointWithExternallyManagedContainer
#pragma warning restore CS0618 // Type or member is obsolete
{
    public Lazy<IMessageSession> MessageSession { get; } = new(() => !endpointCreator.MessageSession.Initialized ? throw new InvalidOperationException("The message session can only be used after the endpoint is started.") : endpointCreator.MessageSession);

    public Task<StartableEndpoint> Create(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        => startupRunner.Create(serviceProvider, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete -- Convert the return type to Task<RunningEndpointInstance> when removing IStartableEndpointWithExternallyManagedContainer
    public async Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken = default) =>
        await startupRunner.Start(externalBuilder, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

    readonly EndpointStartupRunner startupRunner = new(new ExternalContainerEndpointCreationStrategy(endpointCreator));
}