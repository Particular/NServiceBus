namespace NServiceBus.Gateway.Channels
{
    using System;
    using Notifications;

    public interface IChannelReceiver
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;
  
        void Start();
  
        void Stop();
    }
}