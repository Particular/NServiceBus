namespace NServiceBus.Transports.ActiveMQ.Receivers.TransactionsScopes
{
    using System.Transactions;

    using Apache.NMS;

    using NServiceBus.Unicast.Transport;

    public interface ITransactionScopeFactory
    {
        ITransactionScope CreateNewTransactionScope(TransactionSettings transactionSettings, ISession session);

        TransactionScope CreateTransactionScopeForAsyncMessage(TransactionSettings transactionSettings);
    }
}