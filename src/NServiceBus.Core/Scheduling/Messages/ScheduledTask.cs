namespace NServiceBus.Scheduling.Messages
{
    using System;

    [Serializable]
    public class ScheduledTask : IMessage
    {
        public Guid TaskId { get; set; }
    }
}