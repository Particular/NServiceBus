using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Hosting.Azure;
using NServiceBus.Hosting.Roles;
using NServiceBus.Unicast.Config;


namespace NServiceBus.Timeout.Hosting.Azure
{
    using NServiceBus.Azure;

    /// <summary>
    /// Handles configuration related to the timeout manager role
    /// </summary>
    public class TimeoutManagerRoleHandler : IConfigureRole<AsA_TimeoutManager>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a timeout manager on azure
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            if (RoleEnvironment.IsAvailable && !IsHostedIn.ChildHostProcess())
            {
                Configure.Instance.AzureConfigurationSource();
            }

            Configure.Transactions.Enable();
            Configure.Features.Enable<Features.Sagas>();
            Configure.Serialization.Json();

            return Configure.Instance
                .UseAzureTimeoutPersister()
                .UnicastBus()
                    .RunHandlersUnderIncomingPrincipal(false);
        }
    }
}
