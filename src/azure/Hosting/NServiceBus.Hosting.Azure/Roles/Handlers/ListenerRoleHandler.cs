using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Hosting.Roles;
using NServiceBus.Serialization;
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

            if (RoleEnvironment.IsAvailable && !IsHostedIn.ChildHostProcess())
            {
                instance.AzureConfigurationSource();
            }

            if (!instance.Configurer.HasComponent<IMessageSerializer>())
            {
                instance.JsonSerializer();
            }

            Configure.Transactions.Enable();

            return instance
                .UnicastBus()
                .ImpersonateSender(false);
        }
    }
}