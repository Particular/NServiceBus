namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class ScheduleBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public ScheduleBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, CancellationToken, Task> next, CancellationToken cancellationToken)
        {
            if (context.Extensions.TryGet(out State state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next(context, cancellationToken);
        }

        readonly DefaultScheduler scheduler;

        public class State
        {
            public TaskDefinition TaskDefinition { get; set; }
        }
    }
}