namespace NServiceBus.Gateway.Receiving
{
    using System;
    using Channels;
    using Notifications;

    public interface IReceiveMessagesFromSites : IDisposable
    {
        event EventHandler<MessageReceivedOnChannelArgs> MessageReceived;
        void Start(Channel channel, int numWorkerThreads);
    }
}