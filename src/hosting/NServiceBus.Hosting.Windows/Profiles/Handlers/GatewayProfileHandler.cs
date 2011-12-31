namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;
    
    internal class GatewayProfileHandler : IHandleProfile<StartGateway>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunGateway();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
