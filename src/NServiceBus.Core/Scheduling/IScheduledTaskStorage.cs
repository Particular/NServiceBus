namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Generic;

    interface IScheduledTaskStorage
    {
        void Add(ScheduledTask scheduledTask);
        ScheduledTask Get(Guid taskId);
        IDictionary<Guid, ScheduledTask> Tasks { get; }
    }
}