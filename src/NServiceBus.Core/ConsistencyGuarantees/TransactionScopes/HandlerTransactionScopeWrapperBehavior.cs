namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;

    class HandlerTransactionScopeWrapperBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public HandlerTransactionScopeWrapperBehavior(TransactionOptions transactionOptions)
        {
            this.transactionOptions = transactionOptions;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            if (Transaction.Current != null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Required, transactionOptions, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next().ConfigureAwait(false);

                tx.Complete();
            }
        }

        TransactionOptions transactionOptions;
    }
}