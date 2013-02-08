using NServiceBus.Hosting.Profiles;
using NServiceBus.Integration.Azure;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class DevelopmentProfileHandler : IHandleProfile<Development>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (LogManager.LoggerFactory == null)
                Configure.Instance.AzureDiagnosticsLogger(false, !IsHostedIn.ChildHostProcess());
        }
    }
}