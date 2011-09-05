using System.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Hosting.Roles;
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

            if (RoleEnvironment.IsAvailable && IsNotHostedInChildHostProcess())
            {
                instance.AzureConfigurationSource();
            }

            return instance
                .AzureMessageQueue()
                .JsonSerializer()
                .IsTransactional(true)
                .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();
        }

        private static bool IsNotHostedInChildHostProcess()
        {
            var currentProcess = Process.GetCurrentProcess();
            return currentProcess.ProcessName != "NServiceBus.Hosting.Azure.HostProcess" ;
        }
    }
}