namespace NServiceBus
{
    using System.Threading.Tasks;

    class InternallyManagedContainerHost : IStartableEndpoint
    {
        public InternallyManagedContainerHost(IStartableEndpoint startableEndpoint, HostingComponent hostingComponent)
        {
            this.startableEndpoint = startableEndpoint;
            this.hostingComponent = hostingComponent;
        }

        public Task<IEndpointInstance> Start()
        {
            return hostingComponent.Start(startableEndpoint);
        }

        readonly IStartableEndpoint startableEndpoint;
        readonly HostingComponent hostingComponent;
    }
}