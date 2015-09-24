namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToImmediateDispatchConnector : StageConnector<BatchDispatchContext, ImmediateDispatchContext>
    {
        public override Task Invoke(BatchDispatchContext context, Func<ImmediateDispatchContext, Task> next)
        {
            return next(new ImmediateDispatchContext(context.Operations, context));
        }
    }
}