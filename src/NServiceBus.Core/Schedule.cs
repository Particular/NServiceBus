namespace NServiceBus
{
    using System;
    using Scheduling;

    public class Schedule
    {        
        private readonly IScheduler scheduler;
        private readonly ScheduledTask scheduledTask;

        private Schedule(TimeSpan timeSpan)
        {            
            scheduler = Configure.Instance.Builder.Build<IScheduler>();
            scheduledTask = new ScheduledTask { Every = timeSpan };
        }

        public static Schedule Every(TimeSpan timeSpan)
        {
            return new Schedule(timeSpan);
        }

        [Obsolete("Please use the Action overload which uses the TaskName instead")]
        public void Action(Action task)
        {            
            Action(task.Method.DeclaringType.Name, task);
        }

        public void Action(string name, Action task)
        {
            scheduledTask.Task = task;
            scheduledTask.Name = name;
            scheduler.Schedule(scheduledTask);            
        }
    }
}
