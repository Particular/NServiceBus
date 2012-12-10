using NServiceBus;

namespace MyServer.Scheduling
{
    public class ScheduleATask : IMessage
    {        
    }

    public class ScheduledTaskExecuted : IMessage
    {        
    }
}