using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class WithAzureServiceBusQueuesProfileHandler : IHandleProfile<WithAzureServiceBusQueues>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AzureServiceBusMessageQueue();

        }
    }
}