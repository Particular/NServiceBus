namespace NServiceBus.Unicast.Transport.Transactional.DequeueStrategies.ThreadingStrategies
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Utils;

    public class StaticThreadingStrategy:IThreadingStrategy
    {
        public int MaxDegreeOfParallelism 
        { 
            get { return numberOfWorkerThreads; }
        }

        public void ChangeMaxDegreeOfParallelism(int targetNumberOfWorkerThreads)
        {
            lock (workerThreads)
            {
                var current = workerThreads.Count;

                if (targetNumberOfWorkerThreads == current)
                    return;

                if (targetNumberOfWorkerThreads < current)
                {
                    for (var i = targetNumberOfWorkerThreads; i < current; i++)
                        workerThreads[i].Stop();

                    return;
                }

                if (targetNumberOfWorkerThreads > current)
                {
                    for (var i = current; i < targetNumberOfWorkerThreads; i++)
                        AddWorkerThread().Start();
                }
            }
        }

        public void Start(int maxDegreeOfParallelism, Action worker)
        {
            numberOfWorkerThreads = maxDegreeOfParallelism;

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

        WorkerThread AddWorkerThread()
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

        void DoWork()
        {
           workerMethod();
        }

        Action workerMethod;
        int numberOfWorkerThreads = 1;
        readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();
    }
}