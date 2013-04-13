using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the listener role
    /// </summary>
    public class ListenerRoleHandler : IConfigureRole<AsA_Listener>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a listener on azure
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            var instance = Configure.Instance;

            Configure.Transactions.Enable();

            return instance
                .UnicastBus()
                .RunHandlersUnderIncomingPrincipal(false);
        }
    }
}