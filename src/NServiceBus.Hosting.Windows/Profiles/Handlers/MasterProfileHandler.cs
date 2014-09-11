namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    [ObsoleteEx(RemoveInVersion = "6.0")]
    class MasterProfileHandler : IHandleProfile<Master>
    {
        public void ProfileActivated(BusConfiguration config)
        {
            throw new Exception("Master Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, use `NServiceBus.MsmqMaster` profile instead.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("Master Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, use `NServiceBus.MsmqMaster` profile instead.");
        }
    }
}
