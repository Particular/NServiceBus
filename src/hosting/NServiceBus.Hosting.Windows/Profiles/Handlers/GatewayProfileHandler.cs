namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;
    
    internal class GatewayProfileHandler : IHandleProfile<Gateway>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunGateway();
            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
