using System;

namespace NServiceBus.Scheduling.Messages
{
    [Serializable]
    public class ScheduledTask : IMessage
    {
        public Guid TaskId { get; set; }
    }
}