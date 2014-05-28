namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Features;
    using Hosting.Roles;

    class ClientRoleHandler : IConfigureRole<AsA_Client>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a client
        /// </summary>
        public void ConfigureRole(IConfigureThisEndpoint specifier, Configure config)
        {
            config.Transactions.Disable();

            config.Features(f =>
            {
                f.Disable<SecondLevelRetries>();
                f.Disable<StorageDrivenPublishing>();
                f.Disable<TimeoutManager>();
            })
            .PurgeOnStartup(true);
        }
    }
}