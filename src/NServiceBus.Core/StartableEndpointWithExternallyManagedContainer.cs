namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class StartableEndpointWithExternallyManagedContainer : IStartableEndpointWithExternallyManagedContainer
    {
        public StartableEndpointWithExternallyManagedContainer(EndpointCreator endpointCreator, HostingComponent.Configuration hostingConfiguration)
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

        public async Task<IEndpointInstance> Start(IBuilder builder)
        {
            objectBuilder = builder;

            var hostingComponent = HostingComponent.Initialize(hostingConfiguration, null);

            var startableEndpoint = await endpointCreator.CreateStartableEndpoint(builder, hostingComponent).ConfigureAwait(false);

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