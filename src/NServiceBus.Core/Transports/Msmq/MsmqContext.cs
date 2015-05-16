namespace NServiceBus.Transports.Msmq
{
    using System.Threading;

    class MsmqContext
    {
        public AutoResetEvent PeekResetEvent { get; set; }
        public MsmqAddress ErrorQueueAddress { get; set; }
    }
}