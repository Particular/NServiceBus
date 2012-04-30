using System;

namespace NServiceBus.Scheduling
{
    public interface IScheduler
    {
        void Schedule(ScheduledTask task);
        void Start(Guid taskId);
    }
}