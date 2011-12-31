using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NServiceBus.Faults;
using NServiceBus.Hosting.Profiles;
using NServiceBus.Saga;
using NServiceBus.Unicast.Subscriptions;


namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    
    internal class LiteProfileHandler : IHandleProfile<Lite>, IWantTheEndpointConfig, IWantTheListOfActiveProfiles
    {
        void IHandleProfile.ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(StartTimeoutManager)))
                Configure.Instance.UseInMemoryTimeoutPersister();

            if (ActiveProfiles.Contains(typeof(StartGateway)))
                Configure.Instance.UseInMemoryGatewayPersister();

            Configure.Instance.AsMasterNode();

            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.InMemorySagaPersister();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.InMemoryFaultManagement();

            if (Config is AsA_Publisher)
                if (!Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
                    Configure.Instance.InMemorySubscriptionStorage();

            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}