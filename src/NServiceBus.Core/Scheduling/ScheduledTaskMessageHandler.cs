namespace NServiceBus.Scheduling
{
    using System.Threading.Tasks;

    class ScheduledTaskMessageHandler : IHandleMessages<Messages.ScheduledTask>
    {
        DefaultScheduler scheduler;

        public ScheduledTaskMessageHandler(DefaultScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Task Handle(Messages.ScheduledTask message)
        {
            scheduler.Start(message.TaskId);
            return TaskEx.Completed;
        }
    }
}