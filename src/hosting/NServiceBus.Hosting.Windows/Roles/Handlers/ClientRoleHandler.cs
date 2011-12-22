using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Windows.Roles.Handlers
{
    using Unicast.Queuing;

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
            if (!Configure.Instance.Configurer.HasComponent<ISendMessages>())
                Configure.Instance.MsmqTransport();

            return Configure.Instance
                .PurgeOnStartup(true)
                .IsTransactional(false)
                .UnicastBus()
                .ImpersonateSender(false);
        }
    }
}