namespace NServiceBus.Scheduling
{
    using System.Threading.Tasks;
    using Messages;

    class ScheduledTaskMessageHandler : IHandleMessages<ScheduledTask>
    {
        DefaultScheduler scheduler;

        public ScheduledTaskMessageHandler(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Handle(ScheduledTask message, IMessageHandlerContext context)
        {
            scheduler.Start(message.TaskId, context);
            return TaskEx.Completed;
        }
    }
}