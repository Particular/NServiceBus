namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    [ObsoleteEx(RemoveInVersion = "6.0")]
    class GatewayProfileHandler : IHandleProfile<MultiSite>
    {
        public void ProfileActivated(BusConfiguration config)
        {
            throw new Exception("MultiSite Profile is now obsolete. Gateway has been moved to its own stand alone nuget 'NServiceBus.Gateway'. To enable Gateway, install the nuget package and then call `configuration.EnableFeature<Gateway>()`, where `configuration` is an instance of type `BusConfiguration`.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("MultiSite Profile is now obsolete. Gateway has been moved to its own stand alone nuget 'NServiceBus.Gateway'. To enable Gateway, install the nuget package and then call `configuration.EnableFeature<Gateway>()`, where `configuration` is an instance of type `BusConfiguration`.");
        }
    }
}
