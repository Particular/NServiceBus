namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;

    public interface IChannelTypeRegistry
    {
        void AddReceiver(string channelType, Type receiver);
        void AddSender(string channelType, Type sender);

        Type GetReceiverType(string channelType);
        Type GetSenderType(string channelType);

        IEnumerable<string> GetChannelTypesForReceiverType(Type type);
        IEnumerable<string> GetChannelTypesForSenderType(Type type);
    }
}