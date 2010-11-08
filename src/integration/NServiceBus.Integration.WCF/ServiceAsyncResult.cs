using System;
using System.Threading;

namespace NServiceBus
{
    public class ServiceAsyncResult : IAsyncResult
    {
        private readonly object state;
        private volatile bool completed;
        private readonly ManualResetEvent sync;

        /// <summary>
        /// Creates a new object storing the given state.
        /// </summary>
        /// <param name="state"></param>
        public ServiceAsyncResult(object state)
        {
            this.state = state;
            sync = new ManualResetEvent(false);
        }

        /// <summary>
        /// Stores the given completion result from the server, 
        /// releases any blocked threads
        /// </summary>
        public void Complete(CompletionResult result)
        {
            this.Result = result;
            completed = true;
            sync.Set();
        }

        /// <summary>
        /// Returns the original state passed into the Begin method.
        /// </summary>
        public object AsyncState
        {
            get { return this.state; }
        }

        /// <summary>
        /// Returns a handle suitable for blocking threads.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get { return sync; }
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return false; }
        }

        /// <summary>
        /// Contains the completion result from the server.
        /// </summary>
        public CompletionResult Result { get; private set; }

        /// <summary>
        /// Returns if the operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return completed; }
        }
    }
}