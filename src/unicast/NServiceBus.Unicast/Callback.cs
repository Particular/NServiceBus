using System;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.Mvc;

namespace NServiceBus.Unicast
{
    using System.Web.Mvc.Async;

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

        void ICallback.Register<T>(Action<T> callback)
        {
            var page = callback.Target as Page;
            if (page != null)
            {
                (this as ICallback).Register(callback, page);
                return;
            }

            var controller = callback.Target as AsyncController;
            if (controller != null)
            {
                (this as ICallback).Register(callback, controller);
                return;
            }

            var context = SynchronizationContext.Current;
            (this as ICallback).Register(callback, context);
        }

        void ICallback.Register<T>(Action<T> callback, object synchronizer)
        {
            if (!typeof(T).IsEnum && typeof(T) != typeof(int))
                throw new InvalidOperationException("Can only support registering callbacks for integer or enum types. The given type is: " + typeof(T).FullName);

            if (HttpContext.Current != null && synchronizer == null)
                throw new ArgumentNullException("synchronizer", "NServiceBus has detected that you're running in a web context but have passed in a null synchronizer. Please pass in a reference to a System.Web.UI.Page or a System.Web.Mvc.AsyncController.");

            if (synchronizer == null)
            {
                (this as ICallback).Register(GetCallbackInvocationActionFrom(callback), null);
                return;
            }

            if (synchronizer is Page)
            {
                (synchronizer as Page).RegisterAsyncTask(new PageAsyncTask(
                    (sender, e, cb, extraData) => (this as ICallback).Register(cb, extraData),
                    new EndEventHandler(GetCallbackInvocationActionFrom(callback)),
                    null,
                    null
                ));
                return;
            }

            if (synchronizer is AsyncController)
            {
                var am = (synchronizer as AsyncController).AsyncManager;
                am.OutstandingOperations.Increment();

                (this as ICallback).Register(GetMvcCallbackInvocationActionFrom(callback,am), null);

                return;
            }

            if (synchronizer is SynchronizationContext)
            {
                (this as ICallback).Register(
                    ar => (synchronizer as SynchronizationContext).Post(
                              x => GetCallbackInvocationActionFrom(callback).Invoke(ar), null),
                    null
                    );
                return;
            }
        }

        static AsyncCallback GetMvcCallbackInvocationActionFrom<T>(Action<T> callback, AsyncManager am)
        {
            return asyncResult =>
            {
                HandleAsyncResult(callback, asyncResult);
                am.OutstandingOperations.Decrement();
            };
        }

        #endregion

        static AsyncCallback GetCallbackInvocationActionFrom<T>(Action<T> callback)
        {
            return asyncResult => HandleAsyncResult(callback, asyncResult);
        }

        static void HandleAsyncResult<T>(Action<T> callback, IAsyncResult asyncResult)
        {
            var cr = asyncResult.AsyncState as CompletionResult;
            if (cr == null) return;

            if (typeof (T) == typeof (int))
                (callback as Action<int>).Invoke(cr.ErrorCode);
            else
                callback((T) Enum.ToObject(typeof (T), cr.ErrorCode));
        }
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
