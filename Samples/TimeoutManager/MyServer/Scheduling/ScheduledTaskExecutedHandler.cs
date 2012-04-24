using System;
using NServiceBus;

namespace MyServer.Scheduling
{
    public class ScheduledTaskExecutedHandler : IHandleMessages<ScheduledTaskExecuted>
    {
        public void Handle(ScheduledTaskExecuted message)
        {
            Console.WriteLine("ScheduledTaskExecuted handler executed");
        }
    }
}