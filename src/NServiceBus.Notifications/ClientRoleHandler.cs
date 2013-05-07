namespace NServiceBus.Notifications
{
    using NServiceBus.Hosting.Roles;
    using NServiceBus.Unicast.Config;

    /// <summary>
    /// Handles configuration related to the client role
    /// </summary>
    public class ClientRoleHandler : IConfigureRole<AsA_Client>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a client
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            return Configure.Instance.DisableNotifications().UnicastBus();
        }
    }
}