using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using System;

    internal class WithAzureStorageQueuesProfileHandler : IHandleProfile<WithAzureStorageQueues>
    {
        void IHandleProfile.ProfileActivated()
        {
            throw new NotSupportedException("Registering the transport using a profile is no longer supported, please use UsingTransport<WindowsAzureStorageQueue> or UseTransport<WindowsAzureStorageQueue> instead.");
        }
    }
}