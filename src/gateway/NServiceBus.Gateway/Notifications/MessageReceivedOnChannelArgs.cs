namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Channels;
    using Unicast.Transport;

    public class MessageReceivedOnChannelArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public ChannelType FromChannel { get; set; }
        public ChannelType ToChannel { get; set; }
    }
}