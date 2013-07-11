namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Features;
    using Hosting.Profiles;
    
    internal class GatewayProfileHandler : IHandleProfile<MultiSite>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunGateway();
        }
    }
}
