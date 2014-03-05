namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;

    public class ChannelTypeRegistry : IChannelTypeRegistry
    {
        readonly IDictionary<string, Type> receiverTypesByChannelType = new Dictionary<string, Type>();
        readonly IDictionary<string, Type> senderTypesByChannelType = new Dictionary<string, Type>();

        readonly IDictionary<Type, List<string>> channelTypesByReceiverType = new Dictionary<Type, List<string>>();
        readonly IDictionary<Type, List<string>> channelTypesBySenderType = new Dictionary<Type, List<string>>();

        public Type GetReceiverType(string channelType)
        {
            return receiverTypesByChannelType[channelType.ToLower()];
        }

        public Type GetSenderType(string channelType)
        {
            return senderTypesByChannelType[channelType.ToLower()];
        }

        public IEnumerable<string> GetChannelTypesForReceiverType(Type type)
        {
            if (!channelTypesByReceiverType.ContainsKey(type))
            {
                return channelTypesByReceiverType[type];
            }
            return new List<string>();
        }

        public IEnumerable<string> GetChannelTypesForSenderType(Type type)
        {
            if (!channelTypesBySenderType.ContainsKey(type))
            {
                return channelTypesBySenderType[type];
            }
            return new List<string>();
        }

        public void AddReceiver(string channelType, Type receiver)
        {
            receiverTypesByChannelType.Add(channelType.ToLower(), receiver);

            if (!channelTypesByReceiverType.ContainsKey(receiver))
            {
                channelTypesByReceiverType.Add(receiver, new List<string>());
            }
            channelTypesByReceiverType[receiver].Add(channelType.ToLower());
        }

        public void AddSender(string channelType, Type sender)
        {
            senderTypesByChannelType.Add(channelType.ToLower(), sender);

            if (!channelTypesBySenderType.ContainsKey(sender))
            {
                channelTypesBySenderType.Add(sender, new List<string>());
            }
            channelTypesBySenderType[sender].Add(channelType.ToLower());
        }

    }
}