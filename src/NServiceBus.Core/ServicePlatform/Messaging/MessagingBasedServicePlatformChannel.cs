#nullable enable

namespace NServiceBus;

using System.Text.Json.Serialization.Metadata;
using NServiceBus.Routing;
using NServiceBus.ServicePlatform;
using NServiceBus.Transport;

class MessagingBasedServicePlatformChannel(IMessageDispatcher messageDispacther, UnicastAddressTag unicastAddressTag, ReceiveAddresses? receiveAddresses) : ServicePlatformChannel
{
    public override ServicePlatformSender<TMessage> CreateSender<TMessage>(JsonTypeInfo<TMessage> jsonTypeInfo)
        => new MessagingBasedServicePlatformSender<TMessage>(jsonTypeInfo, messageDispacther, unicastAddressTag, receiveAddresses);
}
