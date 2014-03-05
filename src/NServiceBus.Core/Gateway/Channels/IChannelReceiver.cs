namespace NServiceBus.Gateway.Channels
{
    using System;

    public interface IChannelReceiver : IDisposable
    {
        bool RequiresDeduplication { get; }

        event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        void Start(string address, int numberOfWorkerThreads);
    }
}