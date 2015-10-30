namespace NServiceBus.Scheduling
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Logging;

    class DefaultScheduler
    {
        public void Schedule(TaskDefinition taskDefinition)
        {
            scheduledTasks[taskDefinition.Id] = taskDefinition;
        }

        public void Start(Guid taskId, IMessageHandlerContext busContext)
        {
            TaskDefinition taskDefinition;

            if (!scheduledTasks.TryGetValue(taskId, out taskDefinition))
            {
                logger.InfoFormat("Could not find any scheduled task with id {0}. The DefaultScheduler does not persist tasks between restarts.", taskId);
                return;
            }

            DeferTask(taskDefinition, busContext);
            ExecuteTask(taskDefinition);
        }

        static void ExecuteTask(TaskDefinition taskDefinition)
        {
            logger.InfoFormat("Start executing scheduled task named '{0}'.", taskDefinition.Name);
            var sw = new Stopwatch();
            sw.Start();

            Task.Run(taskDefinition.Task)
                .ContinueWith(task =>
                {
                    sw.Stop();

                    if (task.IsFaulted)
                    {
                        task.Exception.Handle(ex =>
                        {
                            logger.Error($"Failed to execute scheduled task '{taskDefinition.Name}'.", ex);
                            return true;
                        });
                    }
                    else
                    {
                        logger.InfoFormat("Scheduled task '{0}' run for {1}", taskDefinition.Name, sw.Elapsed.ToString());
                    }
                }, TaskContinuationOptions.ExecuteSynchronously);
        }

        static void DeferTask(TaskDefinition taskDefinition, IBusContext bus)
        {
            var options = new SendOptions();

            options.DelayDeliveryWith(taskDefinition.Every);
            options.RouteToLocalEndpointInstance();

            bus.SendAsync(new Messages.ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            }, options).GetAwaiter().GetResult();
        }

        static ILog logger = LogManager.GetLogger<DefaultScheduler>();
        internal ConcurrentDictionary<Guid, TaskDefinition> scheduledTasks = new ConcurrentDictionary<Guid, TaskDefinition>();

    }
}