namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;

    class TransactionScopeUnitOfWorkBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public TransactionScopeUnitOfWorkBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, CancellationToken, Task> next, CancellationToken token)
        {
            if (Transaction.Current != null)
            {
                throw new Exception("Ambient transaction detected. The transaction scope unit of work is not supported when there already is a scope present.");
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next(context, token).ConfigureAwait(false);

                tx.Complete();
            }
        }

        readonly TransactionOptions transactionOptions;

        public class Registration : RegisterStep
        {
            public Registration(TransactionOptions transactionOptions) : base("HandlerTransactionScopeWrapper",
                typeof(TransactionScopeUnitOfWorkBehavior),
                "Makes sure that the handlers gets wrapped in a transaction scope",
                b => new TransactionScopeUnitOfWorkBehavior(transactionOptions))
            {
                InsertAfter("ExecuteUnitOfWork");
            }
        }
    }
}