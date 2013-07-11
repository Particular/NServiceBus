namespace MyPublisher.Scheduling
{
    using System;

    using NServiceBus;

    public class ScheduledTaskExecutedHandler : IHandleMessages<ScheduledTaskExecuted>
    {
        public void Handle(ScheduledTaskExecuted message)
        {
            Console.WriteLine("ScheduledTaskExecuted handler executed");
        }
    }
}