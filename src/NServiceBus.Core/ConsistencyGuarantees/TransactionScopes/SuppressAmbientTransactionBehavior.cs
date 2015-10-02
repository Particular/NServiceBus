namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;

    class SuppressAmbientTransactionBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            if (Transaction.Current == null)
            {
                await next().ConfigureAwait(false);
                return;
            }

            using (var tx = new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
            {
                await next().ConfigureAwait(false);

                tx.Complete();
            }
        }
    }
}