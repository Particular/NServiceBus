namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes
{
    using Apache.NMS;

    using NServiceBus.Unicast.Transport.Transactional;

    public interface ITransactionScopeFactory
    {
        ITransactionScope CreateNewTransactionScope(TransactionSettings transactionSettings, ISession session);
    }
}