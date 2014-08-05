namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

#pragma warning disable 437
    [ObsoleteEx(RemoveInVersion = "6.0")]
    class DistributorProfileHandler : IHandleProfile<Distributor>
#pragma warning restore 437
    {
        public void ProfileActivated(ConfigurationBuilder config)
        {
            throw new Exception("The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("The NServiceBus Distributor was moved into its own assembly (NServiceBus.Distributor.MSMQ.dll), please make sure you reference the new assembly.");
        }
    }
}
