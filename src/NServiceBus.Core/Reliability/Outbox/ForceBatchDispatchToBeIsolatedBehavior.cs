namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;
    using Transport;

    class ForceBatchDispatchToBeIsolatedBehavior : Behavior<IBatchDispatchContext>
    {
        public override Task Invoke(IBatchDispatchContext context, Func<Task> next)
        {
            foreach (var operation in context.Operations)
            {
                operation.RequiredDispatchConsistency = DispatchConsistency.Isolated;
            }
            return next();
        }
    }
}