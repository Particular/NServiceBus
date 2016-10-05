namespace NServiceBus
{
    using System.Threading.Tasks;

    class ScheduledTaskMessageHandler : IHandleMessages<ScheduledTask>
    {
        public ScheduledTaskMessageHandler(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Handle(ScheduledTask message, IMessageHandlerContext context)
        {
            return scheduler.Start(message.TaskId, context);
        }

        DefaultScheduler scheduler;
    }
}