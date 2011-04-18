namespace NServiceBus.Gateway.Dispatchers
{
    using System;
    using Channels;

    public interface IChannelFactory
    {
        IChannelSender CreateChannelSender(Type senderType);
    }

    public class DefaultChannelFactory:IChannelFactory
    {
        public IChannelSender CreateChannelSender(Type senderType)
        {
            return Configure.Instance.Builder.Build(senderType) as IChannelSender;
        }
    }
}