using Microsoft.WindowsAzure.ServiceRuntime;
using NServiceBus.Config;
using NServiceBus.Hosting.Azure;
using NServiceBus.Hosting.Roles;
using NServiceBus.Sagas.Impl;
using NServiceBus.Unicast.Config;


namespace NServiceBus.Timeout.Hosting.Azure
{
    using Core;

    /// <summary>
    /// Handles configuration related to the timeout manager role
    /// </summary>
    public class TimeoutManagerRoleHandler : IConfigureRole<AsA_TimeoutManager>, IWantTheEndpointConfig
    {
        /// <summary>
        /// Configures the UnicastBus with typical settings for a timeout manager on azure
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

            return instance
                .JsonSerializer()
                .UseAzureTimeoutPersister()
                .IsTransactional(true)
                .Sagas()
                .UnicastBus()
                    .ImpersonateSender(false);
        }


        public IConfigureThisEndpoint Config { get; set; }
    }
}
