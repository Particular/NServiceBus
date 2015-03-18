namespace NServiceBus.Unicast
{
    using System;
    using System.Threading;

    /// <summary>
    /// Implementation of IAsyncResult returned when registering a callback.
    /// </summary>
    [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0")]
    public class BusAsyncResult : IAsyncResult
    {
        /// <summary>
        /// Creates a new object storing the given callback and state.
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6.0", RemoveInVersion = "7.0")]
        public BusAsyncResult(AsyncCallback callback, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stores the given error code and messages,
        /// releases any blocked threads,
        /// and invokes the previously given callback.
        /// </summary>
        public void Complete(int errorCode, params object[] messages)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a completion result containing the error code, messages, and state.
        /// </summary>
        public object AsyncState
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns a handle suitable for blocking threads.
        /// </summary>
        public WaitHandle AsyncWaitHandle
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool CompletedSynchronously
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns if the operation has completed.
        /// </summary>
        public bool IsCompleted
        {
            get { throw new NotImplementedException(); }
        }
    }
}