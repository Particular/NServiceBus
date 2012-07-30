namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;
    
    class MasterProfileHandler : IHandleProfile<Master>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(Worker)))
                throw new ConfigurationErrorsException("Master profile and Worker profile should not coexist.");

            Configure.Instance.AsMasterNode()
                .RunDistributor()
                .RunGateway();
        }
        
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}
