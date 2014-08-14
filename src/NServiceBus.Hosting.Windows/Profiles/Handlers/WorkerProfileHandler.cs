namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    [ObsoleteEx(RemoveInVersion = "6.0")]
    class WorkerProfileHandler : IHandleProfile<Worker>
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