namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using Pipeline;

    class ScheduleBehavior : IBehavior<IOutgoingLogicalMessageContext, IOutgoingLogicalMessageContext>
    {
        public ScheduleBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Invoke(IOutgoingLogicalMessageContext context, Func<IOutgoingLogicalMessageContext, Task> next)
        {
            if (context.GetOperationProperties().TryGet(out State state))
            {
                scheduler.Schedule(state.TaskDefinition);
            }
            return next(context);
        }

        readonly DefaultScheduler scheduler;

        public class State
        {
            public TaskDefinition TaskDefinition { get; set; }
        }
    }
}