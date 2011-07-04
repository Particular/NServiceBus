namespace NServiceBus.Gateway.Channels
{
    using System;
    using System.Collections.Generic;
    using Http;
    using ObjectBuilder;

    public interface IChannelFactory
    {
        IChannelReceiver GetReceiver(string channelType);
        IChannelSender GetSender(string channelType);
    }

    public class ChannelFactory : IChannelFactory
    {
        readonly IBuilder builder;
        readonly IDictionary<string, Type> receivers = new Dictionary<string, Type>();
        readonly IDictionary<string, Type> senders = new Dictionary<string, Type>();
        
        //public void RegisterReceiver()


        public ChannelFactory(IBuilder builder)
        {
            this.builder = builder;

            //todo - named instances?
            receivers.Add("http", typeof(HttpChannelReceiver));
            senders.Add("http", typeof(HttpChannelSender));
        }

        public IChannelReceiver GetReceiver(string channelType)
        {
            return builder.Build(receivers[channelType.ToLower()]) as IChannelReceiver;
        }

        public IChannelSender GetSender(string channelType)
        {
            return builder.Build(senders[channelType.ToLower()]) as IChannelSender;
        }


    }
}