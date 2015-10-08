namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transports;

    class ForceBatchDispatchToBeIsolatedBehavior : Behavior<BatchDispatchContext>
    {
        public override Task Invoke(BatchDispatchContext context, Func<Task> next)
        {
            foreach (var operation in context.Operations)
            {
                operation.DispatchOptions.RequiredDispatchConsistency = DispatchConsistency.Isolated;
            }
            return next();
        }
    }
}