using System;
using System.Threading;

namespace NServiceBus.Unicast
{
    public class BusAsyncResult : IAsyncResult
    {
        private readonly AsyncCallback callback;
        private readonly CompletionResult result;
        private volatile bool completed;
        private readonly ManualResetEvent sync;

        public BusAsyncResult(AsyncCallback callback, object state)
        {
            this.callback = callback;
            this.result = new CompletionResult();
            this.result.State = state;
            this.sync = new ManualResetEvent(false);
        }

        public void Complete(int errorCode, params IMessage[] messages)
        {
            this.result.ErrorCode = errorCode;
            this.result.Messages = messages;
            this.completed = true;
            this.sync.Set();

            if (this.callback != null)
                this.callback(this);
        }

        #region IAsyncResult Members

        public object AsyncState
        {
            get { return this.result; }
        }

        public WaitHandle AsyncWaitHandle
        {
            get { return this.sync; }
        }

        public bool CompletedSynchronously
        {
            get { return false; }
        }

        public bool IsCompleted
        {
            get { return this.completed; }
        }

        #endregion
    }
}
