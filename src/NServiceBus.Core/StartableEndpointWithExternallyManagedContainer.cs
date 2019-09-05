namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using ObjectBuilder;

    class StartableEndpointWithExternallyManagedContainer : IStartableEndpointWithExternallyManagedContainer
    {
        public StartableEndpointWithExternallyManagedContainer(ConfiguredEndpoint endpoint)
        {
            this.endpoint = endpoint;

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
            endpoint.UseExternallyManagedBuilder(builder);

            var startableEndpoint = await endpoint.CreateStartableEndpoint().ConfigureAwait(false);
            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            messageSession = endpointInstance;

            return endpointInstance;
        }

        ConfiguredEndpoint endpoint;
        IMessageSession messageSession;
    }
}