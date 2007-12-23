using System;

namespace NServiceBus.Unicast
{
    public class Callback : ICallback
    {
        private readonly string messageId;

        public Callback(string messageId)
        {
            this.messageId = messageId;
        }

        public event EventHandler<BusAsyncResultEventArgs> Registered;

        public string MessageId
        {
            get { return messageId; }
        }

        #region ICallback Members

        public IAsyncResult Register(AsyncCallback callback, object state)
        {
            BusAsyncResult result = new BusAsyncResult(callback, state);

            if (this.Registered != null)
                this.Registered(this, new BusAsyncResultEventArgs(result, this.messageId));

            return result;
        }

        #endregion
    }

    public class BusAsyncResultEventArgs : EventArgs
    {
        private readonly BusAsyncResult result;
        private readonly string messageId;
        
        public BusAsyncResultEventArgs(BusAsyncResult result, string messageId)
        {
            this.result = result;
            this.messageId = messageId;
        }

        public BusAsyncResult Result
        {
            get
            {
                return result;
            }
        }

        public string MessageId
        {
            get { return messageId; }
        }
    }
}
