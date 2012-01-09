namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using NServiceBus.Hosting.Profiles;

    internal class TimeoutProfileHandler : IHandleProfile<Time>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance.RunTimeoutManager();
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}
