namespace NServiceBus.Gateway
{
    using System;
    using Unicast.Transport;

    public interface IChannel
    {
        ChannelType Type { get; }
        void Send(TransportMessage msg, string remoteAddress);

        event EventHandler<MessageForwardingArgs> MessageReceived;
        void Start();
        void Stop();
    }

    public enum ChannelType
    {
        Http,
        Msmq
    }
}