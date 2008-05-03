using System;
using System.Threading;
using Common.Logging;

namespace Utils
{
	/// <summary>
	/// The delegate for a method to be run in a loop.
	/// </summary>
    public delegate void Callback();

	/// <summary>
	/// Represents a worker thread that will repeatedly execute a callback.
	/// </summary>
    public class WorkerThread
    {
        private readonly Callback methodToRunInLoop;
        private readonly Thread thread;

		/// <summary>
		/// Initializes a new WorkerThread for the specified method to run.
		/// </summary>
		/// <param name="methodToRunInLoop">The delegate method to execute in a loop.</param>
        public WorkerThread(Callback methodToRunInLoop)
        {
            this.methodToRunInLoop = methodToRunInLoop;
            this.thread = new Thread(this.Loop);
            this.thread.SetApartmentState(ApartmentState.MTA);
            this.thread.Name = String.Format("Worker.{0}", this.thread.ManagedThreadId);
		    this.thread.IsBackground = true;
        }

	    public event EventHandler Stopped;

		/// <summary>
		/// Starts the worker thread.
		/// </summary>
        public void Start()
        {
            if (!this.thread.IsAlive)
                this.thread.Start();
        }

		/// <summary>
		/// Stops the worker thread.
		/// </summary>
        public void Stop()
        {
            lock (_toLock)
                _stopRequested = true;
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
                    this.methodToRunInLoop();
                }
                catch (Exception e)
                {
                    log.Error("Exception reached top level.", e);
                }
            }

            if (this.Stopped != null)
                this.Stopped(this, null);
        }

		/// <summary>
		/// Gets whether or not a stop request has been received.
		/// </summary>
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
        private volatile bool _stopRequested;
        private readonly object _toLock = new object();

        private readonly static ILog log = LogManager.GetLogger(typeof(WorkerThread));
    }
}
