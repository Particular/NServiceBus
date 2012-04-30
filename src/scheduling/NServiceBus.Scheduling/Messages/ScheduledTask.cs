using System;

namespace NServiceBus.Scheduling.Messages
{
    public class ScheduledTask : IMessage
    {
        public Guid TaskId { get; set; }
    }
}