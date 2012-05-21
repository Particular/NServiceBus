using System;
using System.Security.Principal;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Installation;
using NServiceBus.Management.Retries;
using NServiceBus.Utils;

namespace NServiceBus.Config
{        
    public class SecondLevelRetriesConfiguration : IWantToRunBeforeConfigurationIsFinalized, INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Run()
        {
            var retriesConfig = Configure.GetConfigSection<SecondLevelRetriesConfig>();
            var enabled = retriesConfig != null ? retriesConfig.Enabled : true;
            
            if (!Configure.Instance.Configurer.HasComponent<FaultManager>() || !enabled)
            {
                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.Disabled, true);

                return;
            }
                                                  
            if (retriesConfig != null)
            {
                SetUpRetryPolicy(retriesConfig);
            }

            var retriesErrorQ = GetAddress();
                
            // and only when the retries satellite is running should we alter the FaultManager                              
            Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, retriesErrorQ);                
                
            Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)                    
                .ConfigureProperty(rs => rs.InputAddress, retriesErrorQ)
                .ConfigureProperty(rs => rs.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress());                 
        }

        static void SetUpRetryPolicy(SecondLevelRetriesConfig retriesConfig)
        {
            if (retriesConfig.NumberOfRetries != default(int))
            {
                DefaultRetryPolicy.NumberOfRetries = retriesConfig.NumberOfRetries;
            }
            
            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                DefaultRetryPolicy.TimeIncrease = retriesConfig.TimeIncrease;
            }
        }

        public void Install(WindowsIdentity identity)
        {
            MsmqUtilities.CreateQueueIfNecessary(GetAddress(), WindowsIdentity.GetCurrent().Name);
        }
        
        static Address GetAddress()
        {
            return  Address.Parse(Configure.EndpointName).SubScope("Retries");
        }
    }
}