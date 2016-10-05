namespace NServiceBus
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

        public async Task Start(Guid taskId, IPipelineContext context)
        {
            TaskDefinition taskDefinition;

            if (!scheduledTasks.TryGetValue(taskId, out taskDefinition))
            {
                logger.InfoFormat("Could not find any scheduled task with id {0}. The DefaultScheduler does not persist tasks between restarts.", taskId);
                return;
            }

            await DeferTask(taskDefinition, context).ConfigureAwait(false);
            await ExecuteTask(taskDefinition, context).ConfigureAwait(false);
        }

        static async Task ExecuteTask(TaskDefinition taskDefinition, IPipelineContext context)
        {
            logger.InfoFormat("Start executing scheduled task named '{0}'.", taskDefinition.Name);
            var sw = new Stopwatch();
            sw.Start();

            try
            {
                await taskDefinition.Task(context).ConfigureAwait(false);
                logger.InfoFormat("Scheduled task '{0}' run for {1}", taskDefinition.Name, sw.Elapsed);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to execute scheduled task '{taskDefinition.Name}'.", ex);
            }
            finally
            {
                sw.Stop();
            }
        }

        static Task DeferTask(TaskDefinition taskDefinition, IPipelineContext context)
        {
            var options = new SendOptions();

            options.DelayDeliveryWith(taskDefinition.Every);
            options.RouteToThisEndpoint();

            return context.Send(new ScheduledTask
            {
                TaskId = taskDefinition.Id,
                Name = taskDefinition.Name,
                Every = taskDefinition.Every
            }, options);
        }

        ConcurrentDictionary<Guid, TaskDefinition> scheduledTasks = new ConcurrentDictionary<Guid, TaskDefinition>();

        static ILog logger = LogManager.GetLogger<DefaultScheduler>();
    }
}