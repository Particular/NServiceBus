namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

#pragma warning disable 437
    class DistributorProfileHandler : IHandleProfile<Distributor>, IWantTheListOfActiveProfiles
#pragma warning restore 437
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
