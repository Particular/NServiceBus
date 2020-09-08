namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class BatchToDispatchConnector : StageConnector<IBatchDispatchContext, IDispatchContext>
    {
        public override Task Invoke(IBatchDispatchContext context, Func<IDispatchContext, CancellationToken, Task> stage, CancellationToken cancellationToken)
        {
            return stage(this.CreateDispatchContext(context.Operations, context), cancellationToken);
        }
    }
}