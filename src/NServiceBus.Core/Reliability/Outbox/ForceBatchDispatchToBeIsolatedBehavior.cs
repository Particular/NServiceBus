namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Pipeline;
    using Transport;

    class ForceBatchDispatchToBeIsolatedBehavior : IBehavior<IBatchDispatchContext, IBatchDispatchContext>
    {
        public Task Invoke(IBatchDispatchContext context, Func<IBatchDispatchContext, Task> next)
        {
            log.Info($"Outbox forcing {context.Operations.Count} outgoing messages from to be DispatchConsistency.Isolated");
            foreach (var operation in context.Operations)
            {
                operation.RequiredDispatchConsistency = DispatchConsistency.Isolated;
            }
            return next(context);
        }

        static readonly ILog log = LogManager.GetLogger<ForceBatchDispatchToBeIsolatedBehavior>();
    }
}