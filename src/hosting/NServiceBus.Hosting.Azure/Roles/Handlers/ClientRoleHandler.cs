using NServiceBus.Config;
using NServiceBus.Hosting.Roles;
using NServiceBus.Integration.Azure;
using NServiceBus.Unicast.Config;

namespace NServiceBus.Hosting.Azure.Roles.Handlers
{
    /// <summary>
    /// Handles configuration related to the client role
    /// </summary>
    public class ClientRoleHandler : IConfigureRole<AsA_Client>
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a clients on azure
        /// </summary>
        /// <param name="specifier"></param>
        /// <returns></returns>
        public ConfigUnicastBus ConfigureRole(IConfigureThisEndpoint specifier)
        {
            var instance = Configure.Instance;

            instance
                .AzureConfigurationSource()
                .Log4Net<AzureAppender>(
                    a =>
                        {
                            a.ScheduledTransferPeriod = 10;
                        });

            instance = specifier is ICommunicateThroughAppFabricQueues
                           ? instance.AppFabricMessageQueue()
                           : instance.AzureMessageQueue();

            return instance
                .JsonSerializer()
                .IsTransactional(true)
                .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();
        }
    }
}