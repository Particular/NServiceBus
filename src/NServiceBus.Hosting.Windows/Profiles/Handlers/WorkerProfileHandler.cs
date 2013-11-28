namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Hosting.Profiles;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    class WorkerProfileHandler : IHandleProfile<Worker>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {

            if (ActiveProfiles.Contains(typeof(Master)))
            {
                throw new ConfigurationErrorsException("Worker profile and Master profile should not coexist.");
            }

#pragma warning disable 437
            if (ActiveProfiles.Contains(typeof(Distributor)))
            {
                throw new ConfigurationErrorsException("Worker profile and Distributor profile should not coexist.");
            }
#pragma warning restore 437

            Configure.Instance.EnlistWithDistributor();
        }

        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}