using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Common.Logging;

namespace Utils
{
    public delegate void Callback();

    public class WorkerThread
    {
        private Callback methodToRunInLoop;
        private Thread thread;


        public WorkerThread(Callback methodToRunInLoop)
        {
            this.methodToRunInLoop = methodToRunInLoop;
            this.thread = new Thread(new ThreadStart(this.Loop));
            this.thread.SetApartmentState(ApartmentState.MTA);
            this.thread.Name = "Worker";
        }

        public void Start()
        {
            if (!this.thread.IsAlive)
                this.thread.Start();
        }

        public void Stop()
        {
            lock (_toLock)
                _stopRequested = true;
        }

        protected void Loop()
        {
            while (!StopRequested)
            {
                try
                {
                    this.methodToRunInLoop();
                }
                catch (Exception e)
                {
                    log.Error("Exception reached top level.", e);
                }
            }
        }


        protected bool StopRequested
        {
            get
            {
                bool result;
                lock (_toLock)
                    result = _stopRequested;

                return result;
            }
        }
        private bool _stopRequested;
        private object _toLock = new object();

        private static ILog log = LogManager.GetLogger(typeof(WorkerThread));
    }
}
