namespace NServiceBus.Unicast
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    /// <summary>
    /// Implementation of IAsyncResult returned when registering a callback.
    /// </summary>
    public class BusAsyncResult : IAsyncResult
    {
        readonly AsyncCallback callback;
        readonly CompletionResult result;
        readonly TaskCompletionSource<CompletionResult> tcs;
        readonly IAsyncResult internalAsyncResult;

        /// <summary>
        /// Creates a new object storing the given callback and state.
        /// </summary>
        public BusAsyncResult(AsyncCallback callback, object state)
        {
            this.callback = callback;
            result = new CompletionResult
            {
                State = state
            };

            tcs = new TaskCompletionSource<CompletionResult>(result);
            internalAsyncResult = tcs.Task;
        }

        /// <summary>
        /// Stores the given error code and messages,
        /// releases any blocked threads,
        /// and invokes the previously given callback.
        /// </summary>
        public void Complete(int errorCode, params object[] messages)
        {
            result.ErrorCode = errorCode;
            result.Messages = messages;

            if (callback != null)
                try
                {
                    callback(this);
                }
                catch (Exception e)
                {
                    log.Error(callback.ToString(), e);
                    tcs.SetException(e);
                }

            tcs.SetResult(result);
        }

        static ILog log = LogManager.GetLogger<UnicastBus>();

        /// <summary>
        /// Returns a completion result containing the error code, messages, and state.
        /// </summary>
        public object AsyncState
        {
            get { return internalAsyncResult.AsyncState; }
        }

        /// <summary>
        /// Returns a handle suitable for blocking threads.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get { return internalAsyncResult.AsyncWaitHandle; }
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool CompletedSynchronously
        {
            get { return internalAsyncResult.CompletedSynchronously; }
        }

        /// <summary>
        /// Returns if the operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return internalAsyncResult.IsCompleted; }
        }

        internal Task<CompletionResult> Task
        {
            get { return tcs.Task; }
        }
    }
}