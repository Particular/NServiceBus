namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Generic;

    public interface IScheduledTaskStorage
    {
        void Add(ScheduledTask scheduledTask);
        ScheduledTask Get(Guid taskId);
        IDictionary<Guid, ScheduledTask> Tasks { get; }
    }
}