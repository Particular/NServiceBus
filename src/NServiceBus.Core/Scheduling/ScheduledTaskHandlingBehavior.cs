namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class ScheduledTaskHandlingBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public ScheduledTaskHandlingBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
        {
            if (context.Message.Instance is ScheduledTask scheduledTask)
            {
                return scheduler.Start(scheduledTask.TaskId, context);
            }

            return next(context);
        }

        DefaultScheduler scheduler;
    }
}