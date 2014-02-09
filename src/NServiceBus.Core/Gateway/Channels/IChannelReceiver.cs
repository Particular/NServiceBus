namespace NServiceBus.Gateway.Channels
{
    using System;

    public interface IChannelReceiver : IDisposable
    {
        event EventHandler<DataReceivedOnChannelArgs> DataReceived;

        void Start(string address, int numberOfWorkerThreads);
    }
}