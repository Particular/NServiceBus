using NServiceBus.Config;
using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    public class ServerRoleHandler : IConfigureRole<AsA_Server>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a server on azure
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            return Configure.Instance
                .AzureMessageQueue().IsTransactional(true)
                .Sagas()
               // .PurgeOnStartup(false) // todo
                .UnicastBus()
                .ImpersonateSender(true);
        }
    }
}