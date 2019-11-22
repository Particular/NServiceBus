namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

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

            Builder = new Lazy<IBuilder>(() =>
            {
                if (objectBuilder == null)
                {
                    throw new InvalidOperationException("The builder can only be used after the endpoint is started.");
                }
                return objectBuilder;
            });
        }

        public Lazy<IMessageSession> MessageSession { get; private set; }

        internal Lazy<IBuilder> Builder { get; private set; }

        public async Task<IEndpointInstance> Start(IBuilder externalBuilder)
        {
            objectBuilder = externalBuilder;

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration);

            var startableEndpoint = endpointCreator.CreateStartableEndpoint(externalBuilder, hostingComponent);

            hostingComponent.RegisterBuilder(externalBuilder, false);

            await hostingComponent.RunInstallers().ConfigureAwait(false);

            var endpointInstance = await hostingComponent.Start(startableEndpoint).ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        HostingComponent.Configuration hostingConfiguration;
        EndpointCreator endpointCreator;
        IMessageSession messageSession;
        IBuilder objectBuilder;
    }
}