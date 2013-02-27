using NServiceBus.Logging;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using Hosting.Profiles;
    
    [ObsoleteEx(Message = "Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.", RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.0")]
    internal class TimeoutProfileHandler : IHandleProfile<Time>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Log.Warn("Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.");
        }
        private readonly static ILog Log = LogManager.GetLogger(typeof(TimeoutProfileHandler));
        public IConfigureThisEndpoint Config { get; set; }
    }
}
