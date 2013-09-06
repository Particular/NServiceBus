namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Features;
    using Hosting.Roles;
    using Unicast.Config;

    /// <summary>
    /// Handles configuration related to the publisher role
    /// </summary>
    public class PublisherRoleHandler : IConfigureRole<AsA_Publisher>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a publisher
        /// This role is only relevant for the transports that doesn't support native pub/sub like msmq and sqlServer
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            Feature.EnableByDefault<StorageDrivenPublisher>();

            return null;
        }
    }
}