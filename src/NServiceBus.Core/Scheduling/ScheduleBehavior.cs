namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ScheduleBehavior : Behavior<OutgoingLogicalMessageContext>
    {
        DefaultScheduler scheduler;

        public ScheduleBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public class State
        { 
            public TaskDefinition TaskDefinition { get; set; }
        }

        public override Task Invoke(OutgoingLogicalMessageContext context, Func<Task> next)
        {
            State state;
            if (context.TryGet(out state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next();
        }
    }
}