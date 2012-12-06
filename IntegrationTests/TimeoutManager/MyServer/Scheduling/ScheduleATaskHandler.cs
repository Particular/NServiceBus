using System;
using NServiceBus;

namespace MyServer.Scheduling
{
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
            Schedule.Every(TimeSpan.FromMinutes(1)).Action(() => bus.SendLocal(new ScheduledTaskExecuted()));
        }
    }
}