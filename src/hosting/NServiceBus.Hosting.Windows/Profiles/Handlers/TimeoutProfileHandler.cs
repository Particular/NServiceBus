namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using NServiceBus.Hosting.Profiles;

    internal class TimeoutProfileHandler : IHandleProfile<Timeout>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunTimeoutManager();
            WindowsInstallerRunner.RunInstallers = true;
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
