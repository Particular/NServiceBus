namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Channels;
    using NServiceBus.Unicast.Transport;

    public class MessageForwardingArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public ChannelType FromChannel { get; set; }
        public ChannelType ToChannel { get; set; }
    }
}