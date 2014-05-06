namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using Hosting.Profiles;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    class MasterProfileHandler : IHandleProfile<Master>, IWantTheListOfActiveProfiles
    {
        public void ProfileActivated()
        {
            throw new Exception("Obsolete");
        }
        
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}
