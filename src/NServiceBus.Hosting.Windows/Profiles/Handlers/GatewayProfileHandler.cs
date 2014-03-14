namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;
    using Logging;

    [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0", Replacement = "Feature.Enable<Gateway>")]
    class GatewayProfileHandler : IHandleProfile<MultiSite>
    {
        void IHandleProfile.ProfileActivated()
        {
            Log.Warn("MultiSite Profile is obsolete as Gateway is a feature now, you can use Feature.Enable<Gateway> to turn it on.");

            Configure.Instance.RunGateway();
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(GatewayProfileHandler));
    }
}
