namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using System.Collections.Generic;
    using Hosting.Profiles;

#pragma warning disable 437
    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0", Message = "The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.")]
    class DistributorProfileHandler : IHandleProfile<Distributor>, IWantTheListOfActiveProfiles
#pragma warning restore 437
    {
        public void ProfileActivated()
        {
            throw new Exception("Obsolete");
        }
        public IEnumerable<Type> ActiveProfiles { get; set; }
    }
}
