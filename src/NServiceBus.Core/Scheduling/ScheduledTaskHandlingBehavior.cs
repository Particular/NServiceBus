namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Pipeline;

    class ScheduledTaskHandlingBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
    {
        public ScheduledTaskHandlingBehavior(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public async Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next, CancellationToken cancellationToken)
        {
            if (context.Message.Instance is ScheduledTask scheduledTask)
            {
                context.MessageHandled = true;
                await scheduler.Start(scheduledTask.TaskId, context).ConfigureAwait(false);
            }

            await next(context).ConfigureAwait(false);
        }

        DefaultScheduler scheduler;
    }
}