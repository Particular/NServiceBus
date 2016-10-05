namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ScheduleBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public ScheduleBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            State state;
            if (context.Extensions.TryGet(out state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next(context);
        }

        DefaultScheduler scheduler;

        public class State
        {
            public TaskDefinition TaskDefinition { get; set; }
        }
    }
}