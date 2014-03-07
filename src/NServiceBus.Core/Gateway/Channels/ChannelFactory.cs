namespace NServiceBus.Gateway.Channels
{
    using ObjectBuilder;

    public interface IChannelFactory
    {
        IChannelReceiver GetReceiver(string channelType);
        IChannelSender GetSender(string channelType);
    }

    public class ChannelFactory : IChannelFactory
    {
        readonly IChannelTypeRegistry registry;
        readonly IBuilder builder;

        public ChannelFactory(IChannelTypeRegistry registry, IBuilder builder)
        {
            this.registry = registry;
            this.builder = builder;
        }

        public IChannelReceiver GetReceiver(string channelType)
        {
            var receiver = registry.GetReceiverType(channelType);

            return builder.Build(receiver) as IChannelReceiver;
        }

        public IChannelSender GetSender(string channelType)
        {
            var sender = registry.GetSenderType(channelType);

            return builder.Build(sender) as IChannelSender;
        }
    }
}