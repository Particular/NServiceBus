namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class StartableEndpointWithExternallyManagedContainer : IStartableEndpointWithExternallyManagedContainer
    {
        public StartableEndpointWithExternallyManagedContainer(EndpointCreator endpointCreator)
        {
            this.endpointCreator = endpointCreator;

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

            var startableEndpoint = await endpointCreator.CreateStartableEndpoint(builder).ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        EndpointCreator endpointCreator;
        IMessageSession messageSession;
        IBuilder objectBuilder;
    }
}