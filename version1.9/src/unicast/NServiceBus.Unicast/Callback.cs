using System;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Implementation of the ICallback interface for the unicast bus/
    /// </summary>
    public class Callback : ICallback
    {
        private readonly string messageId;

        /// <summary>
        /// Creates a new instance of the callback object storing the given message id.
        /// </summary>
        /// <param name="messageId"></param>
        public Callback(string messageId)
        {
            this.messageId = messageId;
        }

        /// <summary>
        /// Event raised when the Register method is called.
        /// </summary>
        public event EventHandler<BusAsyncResultEventArgs> Registered;

        /// <summary>
        /// Returns the message id this object was constructed with.
        /// </summary>
        public string MessageId
        {
            get { return messageId; }
        }

        #region ICallback Members

        /// <summary>
        /// Returns a new BusAsyncResult storing the given callback and state,
        /// as well as raising the Registered event.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public IAsyncResult Register(AsyncCallback callback, object state)
        {
            BusAsyncResult result = new BusAsyncResult(callback, state);

            if (this.Registered != null)
                this.Registered(this, new BusAsyncResultEventArgs { Result = result, MessageId = this.messageId });

            return result;
        }

        #endregion
    }

    /// <summary>
    /// Argument passed in the Registered event of the Callback object.
    /// </summary>
    public class BusAsyncResultEventArgs : EventArgs
    {
        /// <summary>
        /// Gets/sets the IAsyncResult.
        /// </summary>
        public BusAsyncResult Result { get; set; }

        /// <summary>
        /// Gets/sets the message id.
        /// </summary>
        public string MessageId { get; set; }
    }
}
