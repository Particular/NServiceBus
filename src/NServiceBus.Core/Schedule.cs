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

        [Obsolete("Not compatible with scale-out scenarios, use UniqueAction instead", error: false)]
        public void Action(Action task)
        {            
            Action(task.Method.DeclaringType.Name, task);
        }

        [Obsolete("Not compatible with scale-out scenarios, use UniqueAction instead", error: false)]
        public void Action(string name, Action task)
        {
            scheduledTask.Task = task;
            scheduledTask.Name = name;
            scheduler.Schedule(scheduledTask);            
        }

        /// <summary>
        /// Action that can be executed in scaled-out endpoint
        /// </summary>
        /// <param name="name">Unique name for task action</param>
        /// <param name="task">Scheduled action to execute</param>
        /// <remarks>Throws <exception cref="Exception"> when more than one task action is registed with the same name</exception></remarks>
        public void UniqueAction(string name, Action task)
        {
            scheduledTask.Id = Utils.DeterministicGuid.Create(name);
            scheduledTask.Name = name;
            scheduledTask.Task = task;
            scheduler.ScheduleUnique(scheduledTask);            
        }
    }
}
