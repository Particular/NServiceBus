using NServiceBus.Hosting.Profiles;

namespace NServiceBus.Hosting.Azure.Profiles.Handlers
{
    using System;

    internal class OnSqlAzureProfileHandler : IHandleProfile<OnSqlAzure>, IWantTheEndpointConfig
    {
        void IHandleProfile.ProfileActivated()
        {
            throw new NotSupportedException("Registering the storage infrastructure using a profile is no longer supported, please use UsingTransport<WindowsAzureStorageQueue> or UseTransport<WindowsAzureStorageQueue> instead and override the storage infrastructure using IWantCustomInitialization instead.");
        }

        public IConfigureThisEndpoint Config { get; set; }
    }
}