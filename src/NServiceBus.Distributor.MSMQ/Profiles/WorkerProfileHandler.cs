namespace NServiceBus.Distributor.MSMQ.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    internal class WorkerProfileHandler : IHandleProfile<MSMQWorker>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(MSMQMaster)))
            {
                throw new ConfigurationErrorsException("Worker profile and Master profile should not coexist.");
            }

            if (ActiveProfiles.Contains(typeof(MSMQDistributor)))
            {
                throw new ConfigurationErrorsException("Worker profile and Distributor profile should not coexist.");
            }

            Configure.Instance.EnlistWithMSMQDistributor();
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}