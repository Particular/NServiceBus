namespace NServiceBus.Scheduling
{
    class ScheduledTaskMessageHandler : IHandleMessages<Messages.ScheduledTask>
    {
        DefaultScheduler scheduler;

        public ScheduledTaskMessageHandler(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public void Handle(Messages.ScheduledTask message)
        {
            scheduler.Start(message.TaskId);
        }
    }
}