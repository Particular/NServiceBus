namespace MyPublisher.Scheduling
{
    using System;

    using NServiceBus;

    public class ScheduleATaskHandler : IHandleMessages<ScheduleATask>
    {
        private readonly IBus bus;

        public ScheduleATaskHandler(IBus bus)
        {
            this.bus = bus;
        }

        public void Handle(ScheduleATask message)
        {
            Console.WriteLine("Scheduling a task to be executed every 1 minute");
            Schedule.Every(TimeSpan.FromSeconds(6)).Action(() => this.bus.SendLocal(new ScheduledTaskExecuted()));
        }
    }
}