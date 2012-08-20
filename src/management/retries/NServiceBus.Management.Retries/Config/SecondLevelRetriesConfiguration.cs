using System;
using NServiceBus.Faults.Forwarder;
using NServiceBus.Management.Retries;
using NServiceBus.Unicast.Queuing;

namespace NServiceBus.Config
{        
    public class SecondLevelRetriesConfiguration : IWantToRunBeforeConfigurationIsFinalized
    {
        static bool installQueue;
        public static bool IsDisabled;
        private static Address retriesQueueAddress;
        public ICreateQueues QueueCreator { get; set; }

        public void Run()
        {
            // disabled by configure api
            if (IsDisabled)
            {
                installQueue = false;
                return;
            }
            // if we're not using the Fault Forwarder, we should act as if SLR is disabled
            if (!Configure.Instance.Configurer.HasComponent<FaultManager>())
            {
                DisableSecondLevelRetries();
                installQueue = false;
                return;
            }

            var retriesConfig = Configure.GetConfigSection<SecondLevelRetriesConfig>();
            var enabled = retriesConfig != null ? retriesConfig.Enabled : true;

            // if SLR is disabled from app.config, we should disable SLR, but install the queue
            if (!enabled)
            {
                DisableSecondLevelRetries();
                installQueue = true;
                return;
            }

            installQueue = true;

            SetUpRetryPolicy(retriesConfig);
                            
            // and only when the retries satellite is running should we alter the FaultManager                              
            Configure.Instance.Configurer.ConfigureProperty<FaultManager>(fm => fm.RetriesErrorQueue, RetriesQueueAddress);                
                
            Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                .ConfigureProperty(rs => rs.InputAddress, RetriesQueueAddress)
                .ConfigureProperty(rs => rs.TimeoutManagerAddress, Configure.Instance.GetTimeoutManagerAddress());
        }

        static void DisableSecondLevelRetries()
        {
            Configure.Instance.Configurer.ConfigureComponent<SecondLevelRetries>(DependencyLifecycle.SingleInstance)
                    .ConfigureProperty(rs => rs.Disabled, true);
        }

        static void SetUpRetryPolicy(SecondLevelRetriesConfig retriesConfig)
        {
            if (retriesConfig == null)
                return;

            if (retriesConfig.NumberOfRetries != default(int))
            {
                DefaultRetryPolicy.NumberOfRetries = retriesConfig.NumberOfRetries;
            }
            
            if (retriesConfig.TimeIncrease != TimeSpan.MinValue)
            {
                DefaultRetryPolicy.TimeIncrease = retriesConfig.TimeIncrease;
            }
        }
        
        static Address RetriesQueueAddress 
        {
            get 
            {
                return retriesQueueAddress ?? (retriesQueueAddress = Address.Parse(Configure.EndpointName).SubScope("Retries"));
            }
        }
    }
}