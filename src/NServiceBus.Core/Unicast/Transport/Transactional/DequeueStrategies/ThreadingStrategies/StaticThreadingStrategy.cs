namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies.ThreadingStrategies
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Utils;

    public class StaticThreadingStrategy : IThreadingStrategy
    {
        public void Start(int maximumConcurrencyLevel, Action worker)
        {
            numberOfWorkerThreads = maximumConcurrencyLevel;

            workerMethod = worker;

            for (int i = 0; i < numberOfWorkerThreads; i++)
                AddWorkerThread().Start();
        }

        public void Stop()
        {
            lock (workerThreads)
                for (var i = 0; i < workerThreads.Count; i++)
                    workerThreads[i].Stop();
        }

        private WorkerThread AddWorkerThread()
        {
            lock (workerThreads)
            {
                var result = new WorkerThread(DoWork);

                workerThreads.Add(result);

                result.Stopped += delegate(object sender, EventArgs e)
                    {
                        var wt = sender as WorkerThread;
                        lock (workerThreads)
                            workerThreads.Remove(wt);
                    };

                return result;
            }
        }

        private void DoWork()
        {
            workerMethod();
        }

        private Action workerMethod;
        private int numberOfWorkerThreads = 1;
        private readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();
    }
}