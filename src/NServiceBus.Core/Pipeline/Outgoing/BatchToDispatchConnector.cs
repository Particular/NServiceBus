namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToDispatchConnector : StageConnector<BatchDispatchContext, DispatchContext>
    {
        public override Task Invoke(BatchDispatchContext context, Func<DispatchContext, Task> next)
        {
            return next(new DispatchContext(context.Operations, context));
        }
    }
}