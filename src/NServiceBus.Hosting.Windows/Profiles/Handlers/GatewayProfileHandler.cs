namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;

    class GatewayProfileHandler : IHandleProfile<MultiSite>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunGateway();
        }
    }
}
