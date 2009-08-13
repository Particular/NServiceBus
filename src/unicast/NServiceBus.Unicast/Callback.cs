using System;
using System.Web.UI;

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

        IAsyncResult ICallback.Register(AsyncCallback callback, object state)
        {
            var result = new BusAsyncResult(callback, state);

            if (Registered != null)
                Registered(this, new BusAsyncResultEventArgs { Result = result, MessageId = messageId });

            return result;
        }

        void ICallback.RegisterWebCallback(Action<int> callback, object state)
        {
            var page = callback.Target as Page;
            if (page == null)
                throw new InvalidOperationException(
                    "Callback must be to an object that is a System.Web.UI.Page, or explicitly pass in a reference to the page object.");

            (this as ICallback).RegisterWebCallback(callback, state, page);
        }

        void ICallback.RegisterWebCallback(Action<int> callback, object state, Page page)
        {
            page.RegisterAsyncTask(new PageAsyncTask(
                (sender, e, cb, extraData) => (this as ICallback).Register(cb, extraData),
                asyncResult =>
                    {
                        var cr = asyncResult.AsyncState as CompletionResult;
                        if (cr == null) return;

                        callback(cr.ErrorCode);
                    }, 
                null,
                state
                ));
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
