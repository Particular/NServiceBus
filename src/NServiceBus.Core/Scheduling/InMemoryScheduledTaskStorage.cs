//namespace NServiceBus.Scheduling
//{
//    using System;
//    using System.Collections.Concurrent;
//    using System.Collections.Generic;

//    class InMemoryScheduledTaskStorage 
//    {

//        public void Add(ScheduledTask scheduledTask)
//        {
//            scheduledTasks.Add(scheduledTask.Id, scheduledTask);
//        }

//        public ScheduledTask Get(Guid taskId)
//        {
//            ScheduledTask task;
//            scheduledTasks.TryGetValue(taskId, out task);
//            return task;
//        }

//        public IDictionary<Guid, ScheduledTask> Tasks
//        {
//            get { return scheduledTasks; }            
//        }
//    }
//}