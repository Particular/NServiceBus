using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    internal class WithAzureStorageQueuesProfileHandler : IHandleProfile<WithAzureStorageQueues>
    {
        void IHandleProfile.ProfileActivated()
        {
            Configure.Instance
                .AzureMessageQueue();

        }
    }
}