﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Faults;
    using Hosting.Profiles;
    using Saga;
    using Unicast.Subscriptions;

    internal class IntegrationProfileHandler : IHandleProfile<Integration>, IWantTheEndpointConfig, IWantTheListOfActiveProfiles
    {
        void IHandleProfile.ProfileActivated()
        {
            if (!Configure.Instance.Configurer.HasComponent<IDocumentStore>())
                Configure.Instance.RavenPersistence();

            if (!Configure.Instance.Configurer.HasComponent<IManageMessageFailures>())
                Configure.Instance.MessageForwardingInCaseOfFault();

            if (!Configure.Instance.Configurer.HasComponent<ISagaPersister>())
                Configure.Instance.RavenSagaPersister();


            if (Config is AsA_Publisher && !Configure.Instance.Configurer.HasComponent<ISubscriptionStorage>())
            {
                if ((ActiveProfiles.Contains(typeof(Master))) ||  (ActiveProfiles.Contains(typeof(Worker))) || (ActiveProfiles.Contains(typeof(Distributor))))
                    Configure.Instance.RavenSubscriptionStorage();
                else
                    Configure.Instance.MsmqSubscriptionStorage();
            }

            WindowsInstallerRunner.RunInstallers = true;
            WindowsInstallerRunner.RunInfrastructureInstallers = false;
        }

        public IConfigureThisEndpoint Config { get; set; }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}