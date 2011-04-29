namespace NServiceBus.Gateway.Channels
{
    using System;
    using Notifications;

    public interface IChannelReceiver:IDisposable
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        void Start(string address, int numWorkerThreads);
    }
}