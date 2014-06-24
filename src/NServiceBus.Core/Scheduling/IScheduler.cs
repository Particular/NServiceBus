namespace NServiceBus.Scheduling
{
    using System;

    interface IScheduler
    {
        void Schedule(ScheduledTask task);
        void Start(Guid taskId);
    }
}