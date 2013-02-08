namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes
{
    using Apache.NMS;

    public class NoTransactionScope : ITransactionScope
    {
        public void Dispose()
        {
        }

        public void MessageAccepted(IMessage message)
        {
            message.Acknowledge();
        }

        public void Complete()
        {
        }
    }
}