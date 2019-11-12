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
        }

        public Lazy<IMessageSession> MessageSession { get; private set; }

        public async Task<IEndpointInstance> Start(IBuilder builder)
        {
            var startableEndpoint = await endpointCreator.CreateStartableEndpoint(builder).ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        EndpointCreator endpointCreator;
        IMessageSession messageSession;
    }
}