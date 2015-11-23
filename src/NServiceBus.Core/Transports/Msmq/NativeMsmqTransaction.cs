namespace NServiceBus
{
    using System.Messaging;
    using NServiceBus.Transports;

    class NativeMsmqTransaction : TransportTransaction
    {
        public MessageQueueTransaction MsmqTransaction { get; }

        public NativeMsmqTransaction(MessageQueueTransaction msmqTransaction)
        {
            MsmqTransaction = msmqTransaction;
        }
    }
}