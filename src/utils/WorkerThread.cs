using System;
using System.Threading;
using Common.Logging;

namespace NServiceBus.Utils
{
	/// <summary>
	/// Represents a worker thread that will repeatedly execute a callback.
	/// </summary>
    public class WorkerThread
    {
        private readonly Action methodToRunInLoop;
        private readonly Thread thread;

		/// <summary>
		/// Initializes a new WorkerThread for the specified method to run.
		/// </summary>
		/// <param name="methodToRunInLoop">The delegate method to execute in a loop.</param>
        public WorkerThread(Action methodToRunInLoop)
        {
            this.methodToRunInLoop = methodToRunInLoop;
            thread = new Thread(Loop);
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Name = String.Format("Worker.{0}", thread.ManagedThreadId);
		    thread.IsBackground = true;
        }

        /// <summary>
        /// Event raised when the worker thread has stopped.
        /// </summary>
	    public event EventHandler Stopped;

		/// <summary>
		/// Starts the worker thread.
		/// </summary>
        public void Start()
        {
            if (!thread.IsAlive)
                thread.Start();
        }

		/// <summary>
		/// Stops the worker thread.
		/// </summary>
        public void Stop()
        {
            lock (toLock)
                stopRequested = true;
        }

		/// <summary>
		/// Executes the delegate method until the <see cref="Stop"/>
		/// method is called.
		/// </summary>
        protected void Loop()
        {
            while (!StopRequested)
            {
                try
                {
                    methodToRunInLoop();
                }
                catch (Exception e)
                {
                    Logger.Error("Exception reached top level.", e);
                }
            }

            if (Stopped != null)
                Stopped(this, null);
        }

		/// <summary>
		/// Gets whether or not a stop request has been received.
		/// </summary>
        protected bool StopRequested
        {
            get
            {
                bool result;
                lock (toLock)
                    result = stopRequested;

                return result;
            }
        }

        private volatile bool stopRequested;
        private readonly object toLock = new object();

        private readonly static ILog Logger = LogManager.GetLogger(typeof(WorkerThread));
    }
}
