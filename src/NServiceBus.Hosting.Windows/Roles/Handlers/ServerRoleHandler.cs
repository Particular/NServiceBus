namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Hosting.Roles;
    
    class ServerRoleHandler : IConfigureRole<AsA_Server>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a server
        /// </summary>
        public void ConfigureRole(IConfigureThisEndpoint specifier,Configure config)
        {
            config.ScaleOut(s => s.UseSingleBrokerQueue());
        }
    }
}