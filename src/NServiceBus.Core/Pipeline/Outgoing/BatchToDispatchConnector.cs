namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToDispatchConnector : StageConnector<IBatchDispatchContext, IDispatchContext>
    {
        public override Task Invoke(IBatchDispatchContext context, Func<IDispatchContext, Task> stage)
        {
            return stage(new DispatchContext(context.Operations, context));
        }
    }
}