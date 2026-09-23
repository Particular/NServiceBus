#nullable enable

namespace NServiceBus;

using NServiceBus.ServicePlatform;
using NServiceBus.Settings;
using NServiceBus.Transport;

class MessagingBasedServicePlatformConnection : ServicePlatformConnection
{
    const string DefaultPrimaryServiceControlQueue = "Particular.ServiceControl";

    internal MessagingBasedServicePlatformConnection(
        Configuration platformConfiguration,
        IMessageDispatcher messageDispatcher,
        ReceiveAddresses? receiveAddresses = null
    )
    {
        PrimaryInstance = new MessagingBasedServicePlatformChannel(messageDispatcher, new(platformConfiguration.ServiceControlQueue), receiveAddresses);
    }

    public override ServicePlatformChannel PrimaryInstance { get; }

    internal static void Defaults(SettingsHolder settings)
        => settings.SetDefault(ServiceControlQueueSettingKey, DefaultPrimaryServiceControlQueue);

    internal static Configuration GetConfiguration(IReadOnlySettings settings) => new()
    {
        ServiceControlQueue = settings.Get<string>(ServiceControlQueueSettingKey)
    };

    internal class Configuration
    {
        public required string ServiceControlQueue { get; init; }
    }

    internal const string ServiceControlQueueSettingKey = "ServicePlatform.ServiceControlQueue";
}
