namespace NServiceBus
{
    using System.Threading.Tasks;

    class StartableEndpointWithInternallyManagedContainer : IStartableEndpoint
    {
        public StartableEndpointWithInternallyManagedContainer(IStartableEndpoint startableEndpoint, HostingComponent hostingComponent)
        {
            this.startableEndpoint = startableEndpoint;
            this.hostingComponent = hostingComponent;
        }

        public async Task<IEndpointInstance> Start()
        {
            await hostingComponent.Start().ConfigureAwait(false);

            var endpointInstance = await startableEndpoint.Start().ConfigureAwait(false);

            hostingComponent.AttachRunningEndpoint(endpointInstance);

            return endpointInstance;
        }

        readonly IStartableEndpoint startableEndpoint;
        readonly HostingComponent hostingComponent;
    }
}