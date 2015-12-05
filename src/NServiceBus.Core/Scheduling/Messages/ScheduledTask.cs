namespace NServiceBus
{
    using System;

    [Serializable]
    class ScheduledTask : IMessage
    {
        public Guid TaskId { get; set; }

        public string Name { get; set; }

        public TimeSpan Every { get; set; }
    }
}