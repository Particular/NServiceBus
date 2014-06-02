namespace NServiceBus.Scheduling
{
    class ScheduledTaskMessageHandler : IHandleMessages<Messages.ScheduledTask>
    {
        IScheduler scheduler;

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