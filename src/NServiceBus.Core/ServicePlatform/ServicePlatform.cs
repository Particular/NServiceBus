#nullable enable

namespace NServiceBus;

using System.Text.Json.Serialization.Metadata;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport;

class ServicePlatform(
    ServicePlatform.Configuration platformConfiguration,
    IMessageDispatcher messageDispatcher,
    ReceiveAddresses? receiveAddresses = null
)
{
    const string DefaultPrimaryServiceControlQueue = "Particular.ServiceControl";

    readonly UnicastAddressTag primaryInstanceAddress = new(platformConfiguration.ServiceControlQueue);

    public ServicePlatformSender<TMessage> CreatePrimarySender<TMessage>(JsonTypeInfo<TMessage> jsonTypeInfo)
        => new(jsonTypeInfo, messageDispatcher, primaryInstanceAddress, receiveAddresses);


    internal static void Defaults(SettingsHolder settings)
        => settings.SetDefault(ServiceControlQueueSettingKey, DefaultPrimaryServiceControlQueue);

    internal static Configuration GetConfiguration(IReadOnlySettings settings) => new()
    {
        ServiceControlQueue = settings.Get<string>(ServiceControlQueueSettingKey)
    };

    public class Configuration
    {
        public required string ServiceControlQueue { get; init; }
    }

    public const string ServiceControlQueueSettingKey = "ServicePlatform.ServiceControlQueue";
}
