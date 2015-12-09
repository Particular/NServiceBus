namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using Pipeline;

    class SuppressAmbientTransactionBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
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