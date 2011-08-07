using NServiceBus.Config;
using NServiceBus.Hosting.Roles;
using NServiceBus.Integration.Azure;
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
            var instance = Configure.Instance;

            instance
                .AzureConfigurationSource()
                .Log4Net<AzureAppender>(
                    a =>
                        {
                            a.ScheduledTransferPeriod = 10;
                        });

            return instance
                .AzureMessageQueue()
                .JsonSerializer()
                    .IsTransactional(true)
                .Sagas()
                .UnicastBus()
                    .ImpersonateSender(false)
                    .LoadMessageHandlers();
        }
    }
}