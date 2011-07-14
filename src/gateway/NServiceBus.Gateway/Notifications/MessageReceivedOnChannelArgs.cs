namespace NServiceBus.Gateway.Notifications
{
    using System;
    using Unicast.Transport;

    public class MessageReceivedOnChannelArgs : EventArgs
    {
        public TransportMessage Message { get; set; }
        public string FromChannel { get; set; }
        public string ToChannel { get; set; }
    }
}