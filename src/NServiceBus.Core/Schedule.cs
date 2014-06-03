namespace NServiceBus
{
    using System;
    using Scheduling;

    public class Schedule
    {        
        IScheduler scheduler;
        ScheduledTask scheduledTask;

        Schedule(TimeSpan timeSpan)
        {            
            scheduler = Configure.Instance.Builder.Build<IScheduler>();
            scheduledTask = new ScheduledTask { Every = timeSpan };
        }

        public static Schedule Every(TimeSpan timeSpan)
        {
            return new Schedule(timeSpan);
        }

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
