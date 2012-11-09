namespace NServiceBus.Unicast.Transport.Transactional.ThreadingStrategies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using NServiceBus.Logging;
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

                    return;
                }
            }

        }

        //public int TargetThroughputPerSecond
        //{
        //    get { return maxThroughputPerSecond; }
        //}

        //public void ChangeTargetThroughputPerSecond(int value)
        //{
        //    maxThroughputPerSecond = value;
        //    if (maxThroughputPerSecond == 0)
        //    {
        //        throttlingMilliseconds = 0;
        //        Logger.Debug("Throttling on message receiving rate is not limited by licensing policy.");
        //        return;
        //    }

        //    if (maxThroughputPerSecond > 0)
        //        throttlingMilliseconds = (1000 - AverageMessageHandlingTime) / maxThroughputPerSecond;

        //    Logger.DebugFormat("Setting throttling to: [{0}] message/s per second, sleep between receiving message: [{1}]", maxThroughputPerSecond, throttlingMilliseconds);
        //}

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
            //if (throttlingMilliseconds > 0)
            //    Thread.Sleep(throttlingMilliseconds);

            workerMethod();
        }

         

        Action workerMethod;

        int numberOfWorkerThreads = 1;
        
        readonly IList<WorkerThread> workerThreads = new List<WorkerThread>();

        //static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionalTransport));

        //int throttlingMilliseconds;
        //int maxThroughputPerSecond;
        //const int AverageMessageHandlingTime = 200; // "guessed" time it takes for user code in handler to execute.
    }
}