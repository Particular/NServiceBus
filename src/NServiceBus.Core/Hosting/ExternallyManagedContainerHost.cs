namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    class ExternallyManagedContainerHost : IStartableEndpointWithExternallyManagedContainer
    {
        public ExternallyManagedContainerHost(EndpointCreator endpointCreator)
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

        public async Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken = default)
        {
            objectBuilder = externalBuilder;
            var startableEndpoint = endpointCreator.CreateStartableEndpoint(externalBuilder, serviceProviderIsExternallyManaged: true);
            await startableEndpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
            await startableEndpoint.Setup(cancellationToken).ConfigureAwait(false);
            IEndpointInstance endpointInstance = await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
            messageSession = endpointInstance;
            return endpointInstance;
        }

        EndpointCreator endpointCreator;
        IMessageSession messageSession;
        IServiceProvider objectBuilder;
    }
}