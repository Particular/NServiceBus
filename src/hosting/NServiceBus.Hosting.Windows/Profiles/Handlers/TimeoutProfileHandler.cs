using NServiceBus.Logging;

namespace NServiceBus.Hosting.Windows.Profiles.Handlers
{
    using System;
    using Hosting.Profiles;
    
    [Obsolete("Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.")]
    internal class TimeoutProfileHandler : IHandleProfile<Time>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Log.Warn("Timeout Profile is obsolete as Timeout Manager is on by default for Server and Publisher roles.");
        }
        private readonly static ILog Log = LogManager.GetLogger("TimeoutProfileHandler");
        public IConfigureThisEndpoint Config { get; set; }
    }
}
