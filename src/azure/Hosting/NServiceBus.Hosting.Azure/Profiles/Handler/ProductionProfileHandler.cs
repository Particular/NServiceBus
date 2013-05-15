using NServiceBus.Hosting.Profiles;
using NServiceBus.Azure;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (LogManager.LoggerFactory == null)
                Configure.Instance.AzureDiagnosticsLogger(true, !IsHostedIn.ChildHostProcess());
        }
    }
}