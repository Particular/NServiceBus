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

        public Task RunInstallers()
        {
            return hostingComponent.RunInstallers();
        }

        public Task<IEndpointInstance> Start()
        {
            return hostingComponent.Start(startableEndpoint);
        }

        readonly IStartableEndpoint startableEndpoint;
        readonly HostingComponent hostingComponent;
    }
}