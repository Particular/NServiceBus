using System.Security.Principal;
using NServiceBus.Faults;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Installation;
using NServiceBus.Utils;

namespace NServiceBus.Management.Retries.Config
{
    public class SecondLevelRetriesConfiguration : IWantToRunBeforeConfigurationIsFinalized, INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Run()
        {
            // We can only use this when the FaultForwarder is used            
            var faultManager = Configure.Instance.Builder.Build<IManageMessageFailures>();

            if (faultManager is FaultManager)
            {
                var retriesErrorQ = GetAddress();
                var originalErrorQueue = ((FaultManager) faultManager).ErrorQueue;

                // and only when the retries satellite is running should we alter the FaultManager
                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetriesFaultManager>(DependencyLifecycle.InstancePerCall)
                    .ConfigureProperty(fm => fm.ErrorQueue, retriesErrorQ);

                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.ErrorQueue, originalErrorQueue)
                    .ConfigureProperty(rs => rs.InputAddress, retriesErrorQ)
                    .ConfigureProperty(rs => rs.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress()); 
            }
            else
            {
                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.Disabled, true);
            }             
        }

        public void Install(WindowsIdentity identity)
        {
            MsmqUtilities.CreateQueueIfNecessary(GetAddress(), WindowsIdentity.GetCurrent().Name);
        }
        
        static Address GetAddress()
        {
            Address configuredAddress = null;

            var configSection = Configure.GetConfigSection<SecondLevelRetriesConfig>();
            
            if (configSection != null)
            {
                configuredAddress = Address.Parse(configSection.RetryErrorAddress);
            }

            return configuredAddress ?? Address.Parse(Configure.EndpointName).SubScope("Retries");
        }
    }
}