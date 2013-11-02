namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    class WorkerProfileHandler : IHandleProfile<Worker>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            
            if(ActiveProfiles.Contains(typeof(Master)))
                throw new ConfigurationErrorsException("Worker profile and Master profile should not coexist.");

            if (ActiveProfiles.Contains(typeof(Distributor)))
                throw new ConfigurationErrorsException("Worker profile and Distributor profile should not coexist.");            
            
            Configure.Instance.EnlistWithDistributor();
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}