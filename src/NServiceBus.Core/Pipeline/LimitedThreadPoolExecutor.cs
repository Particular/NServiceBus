namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Logging;

    class LimitedThreadPoolExecutor : IExecutor
    {
        static readonly ILog Logger = LogManager.GetLogger<LimitedThreadPoolExecutor>();
        readonly int maximumConcurrencyLevel;
        [SkipWeaving]
        readonly BusNotifications busNotifications;
        SemaphoreSlim limitSemaphore;
        string[] pipelineIds;
        bool stopping;

        public LimitedThreadPoolExecutor(int maximumConcurrencyLevel, BusNotifications busNotifications)
        {
            this.maximumConcurrencyLevel = maximumConcurrencyLevel;
            this.busNotifications = busNotifications;
            limitSemaphore = new SemaphoreSlim(maximumConcurrencyLevel);
        }

        public virtual void Start(string[] pipelineIds)
        {
            this.pipelineIds = pipelineIds;
        }

        public virtual void Execute(string pipelineId, Action action)
        {
            if (stopping)
            {
                throw new InvalidOperationException("The executor is shutting down.");
            }
            limitSemaphore.Wait();
            if (stopping)
            {
                limitSemaphore.Release();
            }
            try
            {
                busNotifications.Executor.ReportExecutorState(pipelineIds, maximumConcurrencyLevel - limitSemaphore.CurrentCount);
                Task.Factory.StartNew(action)
                    .ContinueWith(x =>
                    {
                        limitSemaphore.Release();
                        if (x.IsFaulted)
                        {
                            Logger.Fatal("Unhandled exception bubbled up to executor", x.Exception);
                        }
                    });
            }
            catch (Exception)
            {
                limitSemaphore.Release(); //We failed to create a task so release here. Something is seriously messed up.
                throw;
            }
        }

        public void ChangeMaximumConcurrencyLevel(int newConcurrencyLevel)
        {
            if (newConcurrencyLevel < 1)
            {
                throw new InvalidOperationException("Maximum concurrency level cannot be less than 1.");
            }
            var difference = Math.Abs(newConcurrencyLevel - maximumConcurrencyLevel);
            if (newConcurrencyLevel > maximumConcurrencyLevel)
            {
                limitSemaphore.Release(difference);
            }
            else
            {
                for (var i = 0; i < difference; i++)
                {
                    limitSemaphore.Wait();
                }
            }
        }

        public virtual void Stop()
        {
            stopping = true;
            for (var index = 0; index < maximumConcurrencyLevel; index++)
            {
                limitSemaphore.Wait();
            }
            limitSemaphore.Release(maximumConcurrencyLevel);
        }

        public virtual void Dispose()
        {
            //Generated
        }
    }
}