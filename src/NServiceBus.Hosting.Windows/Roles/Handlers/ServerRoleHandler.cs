namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Hosting.Roles;
    using Unicast.Config;

    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    public class ServerRoleHandler : IConfigureRole<AsA_Server>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a server
        /// </summary>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            Configure.ScaleOut(s=>s.UseSingleBrokerQueue());

            return Configure.Instance.UnicastBus();
        }
    }
}