namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;

    [ObsoleteEx(RemoveInVersion = "6.0")]
    class GatewayProfileHandler : IHandleProfile<MultiSite>
    {
        public void ProfileActivated(BusConfiguration config)
        {
            throw new Exception("MultiSite Profile is obsolete as Gateway is a feature now, you can use Feature.Enable<Gateway> to turn it on.");
        }

        public void ProfileActivated(Configure config)
        {
            throw new Exception("MultiSite Profile is obsolete as Gateway is a feature now, you can use Feature.Enable<Gateway> to turn it on.");
        }
    }
}
