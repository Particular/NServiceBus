namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;

    class TransactionScopeUnitOfWorkBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public TransactionScopeUnitOfWorkBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            if (Transaction.Current != null)
            {
                throw new Exception("Ambient transaction detected. The transaction scope unit of work is not supported when there already is a scope present.");
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next(context).ConfigureAwait(false);

                tx.Complete();
            }
        }

        TransactionOptions transactionOptions;
    }
}