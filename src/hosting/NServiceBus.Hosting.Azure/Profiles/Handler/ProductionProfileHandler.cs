using NServiceBus.Hosting.Profiles;
using NServiceBus.Integration.Azure;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class ProductionProfileHandler : IHandleProfile<Production>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .Log4Net<AzureAppender>(
                    a =>
                    {
                        a.ScheduledTransferPeriod = 10;
                    });

        }

        
    }
}