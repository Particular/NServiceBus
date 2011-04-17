namespace NServiceBus.Gateway.Sites
{
    using Channels;

    public class Site
    {
        public ChannelType ChannelType { get; set; }

        public string Address { get; set; }

        public string Key { get; set; }
    }
}