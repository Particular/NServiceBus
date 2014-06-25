namespace NServiceBus.Scheduling
{
    using System;

    [ObsoleteEx(RemoveInVersion = "5.1", TreatAsErrorFromVersion = "5.1", Message = "reminder that since this is no longer public we should remove it in the next release.")]
    interface IScheduler
    {
        void Schedule(ScheduledTask task);
        void Start(Guid taskId);
    }
}