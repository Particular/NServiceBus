namespace NServiceBus.Scheduling
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    public class DefaultScheduler : IScheduler
    {
        static ILog logger = LogManager.GetLogger<DefaultScheduler>();

        IBus bus;
        IScheduledTaskStorage scheduledTaskStorage;        

        public DefaultScheduler(IBus bus, IScheduledTaskStorage scheduledTaskStorage)
        {
            this.bus = bus;
            this.scheduledTaskStorage = scheduledTaskStorage;
        }

        public void Schedule(ScheduledTask task)
        {
            scheduledTaskStorage.Add(task);            
            logger.DebugFormat("Task {0}/{1} scheduled with timeSpan {2}", task.Name, task.Id, task.Every);
            DeferTask(task);
        }

        public void Start(Guid taskId)
        {
            var task = scheduledTaskStorage.Get(taskId);

            if (task == null)
            {
                logger.InfoFormat("Could not find any scheduled task {0} with with Id. The DefaultScheduler does not persist tasks between restarts.", taskId);
                return;
            }

            DeferTask(task);
            ExecuteTask(task);
        }

        private static void ExecuteTask(ScheduledTask scheduledTask)
        {
            logger.InfoFormat("Start executing scheduled task {0}", scheduledTask.Name);
            var sw = new Stopwatch();            
            sw.Start();

            Task.Factory
                .StartNew(scheduledTask.Task, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default)
                .ContinueWith(task =>
                    {
                        sw.Stop();

                        if (task.IsFaulted)
                        {
                            task.Exception.Handle(ex =>
                            {
                                logger.Error(String.Format("Failed to execute scheduled task {0}", scheduledTask.Name), ex);
                                return true;
                            });
                        }
                        else
                        {
                            logger.InfoFormat("Scheduled task {0} run for {1}", scheduledTask.Name, sw.Elapsed.ToString());
                        }
                    });
        }

        private void DeferTask(ScheduledTask task)
        {            
            bus.Defer(task.Every, new Messages.ScheduledTask
                {
                    TaskId = task.Id,
                    Name = task.Name,
                    Every = task.Every
                });
        }
    }
}