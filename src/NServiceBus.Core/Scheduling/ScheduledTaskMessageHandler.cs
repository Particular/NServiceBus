namespace NServiceBus.Scheduling
{
    public class ScheduledTaskMessageHandler : IHandleMessages<Messages.ScheduledTask>
    {
        private readonly IScheduler scheduler;

        public ScheduledTaskMessageHandler(IScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public void Handle(Messages.ScheduledTask message)
        {
            scheduler.Start(message.TaskId);
        }
    }
}