using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the server role
    /// </summary>
    public class WorkerRoleHandler : IConfigureRole<AsA_Worker>, IWantTheEndpointConfig
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a server on azure
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            var instance = Configure.Instance;

            Configure.Transactions.Enable();
            
            return instance
                .Sagas()
                .UnicastBus()
                    .RunHandlersUnderIncomingPrincipal(false);
        }


        public IConfigureThisEndpoint Config { get; set; }
    }
}
                    