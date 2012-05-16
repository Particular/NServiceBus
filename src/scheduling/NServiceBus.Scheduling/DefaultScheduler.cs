using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NServiceBus.Logging;

namespace NServiceBus.Scheduling
{
    public class DefaultScheduler : IScheduler
    {
        static readonly ILog logger = LogManager.GetLogger(typeof(DefaultScheduler));

        private readonly IBus bus;
        private readonly IScheduledTaskStorage scheduledTaskStorage;        

        public DefaultScheduler(IBus bus, IScheduledTaskStorage scheduledTaskStorage)
        {
            this.bus = bus;
            this.scheduledTaskStorage = scheduledTaskStorage;
        }

        public void Schedule(ScheduledTask task)
        {
            scheduledTaskStorage.Add(task);            
            logger.DebugFormat("Task {0}/{1} scheduled with timespan {2}", task.Name, task.Id, task.Every);
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
                .StartNew(scheduledTask.Task, TaskCreationOptions.None)
                .ContinueWith(_ =>
                                  {
                                      sw.Stop();
                                      logger.InfoFormat("Scheduled task {0} run for {1}", scheduledTask.Name, sw.Elapsed.ToString());
                                  });
        }

        private void DeferTask(ScheduledTask task)
        {            
            bus.Defer(task.Every, new Messages.ScheduledTask { TaskId = task.Id });
        }
    }
}