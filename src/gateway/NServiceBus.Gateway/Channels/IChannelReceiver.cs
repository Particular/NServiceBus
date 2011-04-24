namespace NServiceBus.Gateway.Channels
{
    using System;
    using Notifications;

    //todo - implement IDisposable to enable cleanup
    public interface IChannelReceiver
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;

        void Start(string address, int numWorkerThreads);

        void Stop();
    }
}