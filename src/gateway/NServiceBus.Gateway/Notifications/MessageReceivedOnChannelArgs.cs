namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Unicast.Transport;

    public class MessageReceivedOnChannelArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public Type FromChannel { get; set; }
        public Type ToChannel { get; set; }
    }
}