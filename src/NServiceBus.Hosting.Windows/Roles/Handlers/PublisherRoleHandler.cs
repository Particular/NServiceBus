namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Hosting.Roles;
    
    class PublisherRoleHandler : IConfigureRole<AsA_Publisher>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a publisher
        /// This role is only relevant for the transports that doesn't support native pub/sub like msmq and sqlServer
        /// </summary>
        public void ConfigureRole(IConfigureThisEndpoint specifier,Configure config)
        {
        }
    }
}