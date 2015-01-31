namespace NServiceBus.Scheduling
{
    using System;

    public interface IScheduler
    {
        void Schedule(ScheduledTask task);
        void Start(Guid taskId);
        void ScheduleUnique(ScheduledTask scheduledTask);
    }
}