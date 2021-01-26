namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;

    class InternallyManagedContainerHost : IStartableEndpoint
    {
        public InternallyManagedContainerHost(IStartableEndpoint startableEndpoint, HostingComponent hostingComponent)
        {
            this.startableEndpoint = startableEndpoint;
            this.hostingComponent = hostingComponent;
        }

        public Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
        {
            return hostingComponent.Start(startableEndpoint, cancellationToken);
        }

        readonly IStartableEndpoint startableEndpoint;
        readonly HostingComponent hostingComponent;
    }
}