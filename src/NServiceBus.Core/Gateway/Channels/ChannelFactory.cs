namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public interface IChannelFactory
    {
        IChannelReceiver GetReceiver(string channelType);
        IChannelSender GetSender(string channelType);
    }

    public class ChannelFactory : IChannelFactory
    {
        public IChannelReceiver GetReceiver(string channelType)
        {
            var receiver = receivers[channelType.ToLower()];

            return Configure.Instance.Builder.Build(receiver) as IChannelReceiver;
        }

        public IChannelSender GetSender(string channelType)
        {
            var sender = senders[channelType.ToLower()];

            return Configure.Instance.Builder.Build(sender) as IChannelSender;
        }


        public void RegisterReceiver(Type receiver)
        {
            RegisterReceiver(receiver, receiver.Name.Substring(0, receiver.Name.IndexOf("Channel")));
        }

        public void RegisterReceiver(Type receiver, string type)
        {
            receivers.Add(type.ToLower(), receiver);

            if (!Configure.HasComponent(receiver))
            {
                Configure.Component(receiver, DependencyLifecycle.InstancePerCall);
            }
        }


        public void RegisterSender(Type sender)
        {
            var channelTypes =
                sender.GetCustomAttributes(true).OfType<ChannelTypeAttribute>().ToList();
            if (channelTypes.Any())
            {
                channelTypes.ForEach(type => RegisterSender(sender, type.Type));
            }
            else
            {
                RegisterSender(sender, sender.Name.Substring(0, sender.Name.IndexOf("Channel")));
            }
        }

        public void RegisterSender(Type sender, string type)
        {
            senders.Add(type.ToLower(), sender);

            if (!Configure.HasComponent(sender))
            {
                Configure.Component(sender, DependencyLifecycle.InstancePerCall);
            }
        }

        readonly IDictionary<string, Type> receivers = new Dictionary<string, Type>();
        readonly IDictionary<string, Type> senders = new Dictionary<string, Type>();
    }
}