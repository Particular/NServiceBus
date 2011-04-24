namespace NServiceBus.Gateway.Channels
{
    using System;

    public class Channel
    {
        public Type Receiver { get; set; }
        public string ReceiveAddress { get; set; }
        public int NumWorkerThreads { get; set; }
    }
}