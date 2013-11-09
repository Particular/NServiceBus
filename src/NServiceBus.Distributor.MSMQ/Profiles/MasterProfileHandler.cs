namespace NServiceBus.Distributor.MSMQ.Profiles
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Features;
    using Hosting.Profiles;

    internal class MasterProfileHandler : IHandleProfile<MSMQMaster>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(MSMQWorker)))
            {
                throw new ConfigurationErrorsException("Master profile and Worker profile should not coexist.");
            }

            Configure.Instance.AsMSMQMasterNode();

            Feature.EnableByDefault<Gateway>();
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}