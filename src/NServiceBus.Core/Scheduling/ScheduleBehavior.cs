namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ScheduleBehavior : Behavior<IOutgoingLogicalMessageContext>
    {
        public ScheduleBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            State state;
            if (context.Extensions.TryGet(out state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next();
        }

        DefaultScheduler scheduler;

        public class State
        {
            public TaskDefinition TaskDefinition { get; set; }
        }
    }
}