namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.OutgoingPipeline;

    class ScheduleBehavior : Behavior<IOutgoingLogicalMessageContext>
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

        public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
        {
            State state;
            if (context.Extensions.TryGet(out state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next();
        }
    }
}