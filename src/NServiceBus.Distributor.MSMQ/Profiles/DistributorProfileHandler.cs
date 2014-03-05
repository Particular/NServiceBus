namespace NServiceBus.Distributor.MSMQ.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    internal class DistributorProfileHandler : IHandleProfile<MSMQDistributor>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(MSMQWorker)))
            {
                throw new ConfigurationErrorsException("Distributor profile and Worker profile should not coexist.");
            }

            Configure.Instance.RunMSMQDistributor(false);
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}