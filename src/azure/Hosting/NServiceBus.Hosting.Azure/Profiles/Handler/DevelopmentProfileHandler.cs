using NServiceBus.Hosting.Profiles;
using log4net.Appender;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class DevelopmentProfileHandler : IHandleProfile<Development>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .Log4Net<ConsoleAppender>( a => { });

        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}