using NServiceBus.Hosting.Profiles;
using NServiceBus.Azure;
using NServiceBus.Logging;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using Logging.Loggers;

    internal class ProductionProfileHandler : IHandleProfile<Production>
    {
        void IHandleProfile.ProfileActivated()
        {
            if (LogManager.LoggerFactory is NullLoggerFactory)
                Configure.Instance.AzureDiagnosticsLogger(true, !IsHostedIn.ChildHostProcess());
        }
    }
}