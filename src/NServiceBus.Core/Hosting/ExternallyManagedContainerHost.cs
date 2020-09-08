namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class ExternallyManagedContainerHost : IStartableEndpointWithExternallyManagedContainer
    {
        public ExternallyManagedContainerHost(EndpointCreator endpointCreator, HostingComponent.Configuration hostingConfiguration)
        {
            this.endpointCreator = endpointCreator;
            this.hostingConfiguration = hostingConfiguration;

            MessageSession = new Lazy<IMessageSession>(() =>
            {
                if (messageSession == null)
                {
                    throw new InvalidOperationException("The message session can only be used after the endpoint is started.");
                }
                return messageSession;
            });

            Builder = new Lazy<IServiceProvider>(() =>
            {
                if (objectBuilder == null)
                {
                    throw new InvalidOperationException("The builder can only be used after the endpoint is started.");
                }
                return objectBuilder;
            });
        }

        public Lazy<IMessageSession> MessageSession { get; private set; }

        internal Lazy<IServiceProvider> Builder { get; private set; }

        public async Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken)
        {
            objectBuilder = externalBuilder;

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var startableEndpoint = endpointCreator.CreateStartableEndpoint(externalBuilder, hostingComponent);

            hostingComponent.RegisterBuilder(externalBuilder, false);

            await hostingComponent.RunInstallers().ConfigureAwait(false);

            var endpointInstance = await hostingComponent.Start(startableEndpoint, cancellationToken).ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        HostingComponent.Configuration hostingConfiguration;
        EndpointCreator endpointCreator;
        IMessageSession messageSession;
        IServiceProvider objectBuilder;
    }
}