namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Features;
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
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier, Configure configure)
        {
            configure.Transactions.Disable();
            configure.Features.Disable<SecondLevelRetries>();
            configure.Features.Disable<StorageDrivenPublisher>();
            return configure
                            .PurgeOnStartup(true)
                            .DisableTimeoutManager()
                            .UnicastBus();

        }
    }
}