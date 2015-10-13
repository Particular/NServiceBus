namespace NServiceBus
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
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

        public void Action(Action task)
        {
            var declaringType = task.Method.DeclaringType;

            while (declaringType.DeclaringType != null && declaringType.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Any())
            {
                declaringType = declaringType.DeclaringType;
            }

            Action(declaringType.Name, task);
        }

        public void Action(string name, Action task)
        {
            scheduledTask.Task = task;
            scheduledTask.Name = name;
            scheduler.Schedule(scheduledTask);            
        }
    }
}
