namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class InMemoryScheduledTaskStorage : IScheduledTaskStorage
    {
        protected readonly IDictionary<Guid, ScheduledTask> scheduledTasks = new ConcurrentDictionary<Guid, ScheduledTask>();

        public void Add(ScheduledTask scheduledTask)
        {
            scheduledTasks.Add(scheduledTask.Id, scheduledTask);
        }

        public ScheduledTask Get(Guid taskId)
        {
            if (scheduledTasks.ContainsKey(taskId))
                return scheduledTasks[taskId];

            return null;
        }

        public IDictionary<Guid, ScheduledTask> Tasks
        {
            get { return scheduledTasks; }            
        }
    }
}