namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    [ObsoleteEx(RemoveInVersion = "6.0")]
    class WorkerProfileHandler : IHandleProfile<Worker>
    {
        public void ProfileActivated(BusConfiguration config)
        {
            throw new Exception("Worker Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, use `NServiceBus.MsmqWorker` profile instead.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("Worker Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, use `NServiceBus.MsmqWorker` profile instead.");            
        }
    }
}