using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using System;

    internal class WithAzureServiceBusQueuesProfileHandler : IHandleProfile<WithAzureServiceBusQueues>
    {
        void IHandleProfile.ProfileActivated()
        {
            throw new NotSupportedException("Registering the transport using a profile is no longer supported, please use UsingTransport<WindowsAzureServiceBus> or UseTransport<WindowsAzureServiceBus> instead.");
        }
    }
}