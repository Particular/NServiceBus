namespace NServiceBus.Transport.ActiveMQ.Receivers.TransactonsScopes
{
    using System;
    using System.Transactions;
    using Apache.NMS;

    public class DTCTransactionScope : ITransactionScope
    {
        private readonly TransactionScope transactionScope;
        private bool complete;

        public DTCTransactionScope(ISession session, TransactionOptions transactionOptions)
        {
            this.transactionScope = new TransactionScope(TransactionScopeOption.Required, transactionOptions);
            //((NetTxSession)session).Enlist(Transaction.Current);
        }

        public void Dispose()
        {
            this.transactionScope.Dispose();
            if (!this.complete) throw new Exception();
        }

        public void MessageAccepted(IMessage message)
        {
        }

        public void Complete()
        {
            this.complete = true;
            this.transactionScope.Complete();
        }
    }
}