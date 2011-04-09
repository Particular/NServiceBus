namespace NServiceBus.Gateway.Channels
{
    using System;
    using Notifications;

    public interface IChannelReceiver
    {
        ChannelType Type { get; }
  
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