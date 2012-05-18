using System;
using System.Security.Principal;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Installation;
using NServiceBus.Management.Retries;
using NServiceBus.Utils;

namespace NServiceBus.Config
{        
    public class SecondLevelRetriesConfiguration : INeedInitialization, INeedToInstallSomething<Installation.Environments.Windows>
    {
        public void Init()
        {
            var retriesConfig = Configure.GetConfigSection<SecondLevelRetriesConfig>();
            var enabled = retriesConfig != null ? retriesConfig.Enabled : true;

            Address errorQueue = null;

            if (enabled)
            {
                var forwardingInCaseOfFaultConfig = Configure.GetConfigSection<MessageForwardingInCaseOfFaultConfig>();

                if (forwardingInCaseOfFaultConfig != null)
                {
                    errorQueue = Address.Parse(forwardingInCaseOfFaultConfig.ErrorQueue);
                }
            }

            if (errorQueue != null)
            {
                if (retriesConfig != null)
                {
                    SetUpRetryPolicy(retriesConfig);
                }

                var retriesErrorQ = GetAddress();
                var originalErrorQueue = errorQueue;
                
                // and only when the retries satellite is running should we alter the FaultManager                              
                Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.ErrorQueue, retriesErrorQ);
                Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.RealErrorQueue, originalErrorQueue);
                
                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.ErrorQueue, originalErrorQueue)
                    .ConfigureProperty(rs => rs.InputAddress, retriesErrorQ)
                    .ConfigureProperty(rs => rs.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress()); 

                
            }
            else
            {
                Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.Disabled, true);

                Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.RealErrorQueue, null);
            }             
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