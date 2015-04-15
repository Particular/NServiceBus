namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    class DefaultScheduler
    {
        public DefaultScheduler(IBus bus)
        {
            this.bus = bus;
        }

        public void Schedule(TaskDefinition taskDefinition)
        {
            scheduledTasks[taskDefinition.Id] = taskDefinition;
            logger.DebugFormat("Task '{0}' (with id {1}) scheduled with timeSpan {2}", taskDefinition.Name, taskDefinition.Id, taskDefinition.Every);
            DeferTask(taskDefinition);
        }

        public void Start(Guid taskId)
        {
            TaskDefinition taskDefinition;

            if (!scheduledTasks.TryGetValue(taskId, out taskDefinition))
            {
                logger.InfoFormat("Could not find any scheduled task with id {0}. The DefaultScheduler does not persist tasks between restarts.", taskId);
                return;
            }

            DeferTask(taskDefinition);
            ExecuteTask(taskDefinition);
        }

        static void ExecuteTask(TaskDefinition taskDefinition)
        {
            logger.InfoFormat("Start executing scheduled task named '{0}'.", taskDefinition.Name);
            var sw = new Stopwatch();
            sw.Start();

            Task.Factory
                .StartNew(taskDefinition.Task, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
                .ContinueWith(task =>
                {
                    sw.Stop();

                    if (task.IsFaulted)
                    {
                        task.Exception.Handle(ex =>
                        {
                            logger.Error(String.Format("Failed to execute scheduled task '{0}'.", taskDefinition.Name), ex);
                            return true;
                        });
                    }
                    else
                    {
                        logger.InfoFormat("Scheduled task '{0}' run for {1}", taskDefinition.Name, sw.Elapsed.ToString());
                    }
                });
        }

        void DeferTask(TaskDefinition taskDefinition)
        {
            bus.SendLocal(new Messages.ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            }, new SendLocalOptions(delayDeliveryFor: taskDefinition.Every));
        }

        static ILog logger = LogManager.GetLogger<DefaultScheduler>();
        IBus bus;
        internal ConcurrentDictionary<Guid, TaskDefinition> scheduledTasks = new ConcurrentDictionary<Guid, TaskDefinition>();
    }
}