namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Features;
    using Hosting.Profiles;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    class MasterProfileHandler : IHandleProfile<Master>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            if (ActiveProfiles.Contains(typeof(Worker)))
            {
                throw new ConfigurationErrorsException("Master profile and Worker profile should not coexist.");
            }

            Configure.Instance.RunDistributor();

            Feature.EnableByDefault<Gateway>();
        }
        
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}
