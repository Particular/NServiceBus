using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    class DistributorProfileHandler : IHandleProfile<Distributor>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(Worker)))
                throw new ConfigurationErrorsException("Distributor profile and Worker profile should not coexist.");

            Configure.Instance.AsMasterNode()
                .RunDistributorWithNoWorkerOnItsEndpoint();
        }
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}
