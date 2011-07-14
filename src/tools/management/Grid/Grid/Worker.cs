using System;

namespace Grid
{
    [Serializable]
    public class Worker
    {
        private string queue;
        public string Queue
        {
            get { return queue; }
            set { queue = value; }
        }

        private int numberOfWorkerThreads;
        public int NumberOfWorkerThreads
        {
            get
            {
                lock(this)
                    return numberOfWorkerThreads;
            }
        }

        public override string ToString()
        {
            return this.queue;
        }

        public void SetNumberOfWorkerThreads(int number)
        {
            lock (this)
                this.numberOfWorkerThreads = number;
        }
 
    }
}
