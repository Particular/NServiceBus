namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

#pragma warning disable 437
    [ObsoleteEx(RemoveInVersion = "6.0")]
    class DistributorProfileHandler : IHandleProfile<Distributor>
#pragma warning restore 437
    {
        public void ProfileActivated(BusConfiguration config)
        {
            throw new Exception("Distributor Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, then use the `NServiceBus.MsmqDistributor` profile instead.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("Distributor Profile is now obsolete. The distributor feature has been moved to its own stand alone nuget 'NServiceBus.Distributor.MSMQ'. Once you've installed this package, then use the `NServiceBus.MsmqDistributor` profile instead.");
        }
    }
}
