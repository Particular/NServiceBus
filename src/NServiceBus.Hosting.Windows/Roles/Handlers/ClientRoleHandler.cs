namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Hosting.Roles;
    using Unicast.Config;

    /// <summary>
    /// Handles configuration related to the client role
    /// </summary>
    public class ClientRoleHandler : IConfigureRole<AsA_Client>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a client
        /// </summary>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            Configure.Transactions.Disable();
            Configure.Features.Disable<Features.SecondLevelRetries>();

            return Configure.Instance
                            .PurgeOnStartup(true)
                            .DisableTimeoutManager()
                            .UnicastBus()
                            .RunHandlersUnderIncomingPrincipal(false);

        }
    }
}