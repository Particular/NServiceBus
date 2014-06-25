namespace NServiceBus.Scheduling
{
    using System;

    [ObsoleteEx(TreatAsErrorFromVersion = "5.0", RemoveInVersion = "5.1", Message = "The Schedule is now injectable, This won't be needed.")]
    public class ScheduledTask
    {
        public ScheduledTask()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; private set; }
        public string Name { get; set; }
        public Action Task { get; set; }
        public TimeSpan Every { get; set; }
    }
}