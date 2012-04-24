using System;
using System.Collections.Generic;

namespace NServiceBus.Scheduling
{
    public interface IScheduledTaskStorage
    {
        void Add(ScheduledTask scheduledTask);
        ScheduledTask Get(Guid taskId);
        IDictionary<Guid, ScheduledTask> Tasks { get; }
    }
}