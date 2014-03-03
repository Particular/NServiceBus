namespace NServiceBus.Scheduling
{
    using System;

    [Serializable]
    public class ScheduledTask
    {
        public ScheduledTask()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }
        public string Name { get; set; }
        public Action Task { get; set; }
        public TimeSpan Every { get; set; }
    }
}
