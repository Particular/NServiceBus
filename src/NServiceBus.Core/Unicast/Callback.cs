namespace NServiceBus
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using System.Web.UI;
    using NServiceBus.Logging;
    using Unicast;

    /// <summary>
    /// Implementation of the <see cref="ICallback"/> interface for the unicast bus.
    /// </summary>
    class Callback : ICallback
    {
        static ILog log = LogManager.GetLogger<UnicastBus>();
        static Type AsyncControllerType;

        string messageId;
        bool isSendOnly;

        static Callback()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies.Where(assembly => assembly.GetName().Name == "System.Web.Mvc"))
            {
                AsyncControllerType = assembly.GetType("System.Web.Mvc.AsyncController", false);
            }

            if (AsyncControllerType == null)
            {
                //We just initialize it to any type so we don't need to check for nulls.
                AsyncControllerType = typeof(BusAsyncResultEventArgs);
            }
        }

        /// <summary>
        /// Creates a new instance of the callback object storing the given message id.
        /// </summary>
        public Callback(string messageId, bool isSendOnly)
        {
            this.messageId = messageId;
            this.isSendOnly = isSendOnly;
        }

        /// <summary>
        /// Event raised when the Register method is called.
        /// </summary>
        public event EventHandler<CallbackResultEventArgs> Registered;

        /// <summary>
        /// Returns the message id this object was constructed with.
        /// </summary>
        public string MessageId
        {
            get { return messageId; }
        }

        public Task<int> Register()
        {
            return InternalRegister(null, null)
                .ContinueWith(t => t.Result.ErrorCode, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task<T> Register<T>()
        {
            if (!typeof(T).IsEnum)
                throw new InvalidOperationException(
                    "Register<T> can only be used with enumerations, use Register() to return an integer instead");

            return InternalRegister(null, null)
                .ContinueWith(t => (T)Enum.Parse(typeof(T), t.Result.ErrorCode.ToString(CultureInfo.InvariantCulture)),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task<T> Register<T>(Func<CompletionResult, T> completion)
        {
            return InternalRegister(null, null).ContinueWith(t => completion(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public Task Register(Action<CompletionResult> completion)
        {
            return InternalRegister(null, null).ContinueWith(t => completion(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        public IAsyncResult Register(AsyncCallback callback, object state)
        {
            return InternalRegister(callback, state);
        }

        Task<CompletionResult> InternalRegister(AsyncCallback callback, object state)
        {
            if (isSendOnly)
            {
                throw new Exception("Callbacks are invalid in a sendonly endpoint.");
            }

            var tcs = new TaskCompletionSource<CompletionResult>(new CompletionResult
            {
                State = state
            });

            if (callback != null)
            {
                tcs.Task.ContinueWith(t => callback(t));
            }

            var handler = Registered;
            if (handler != null)
                handler(this, new CallbackResultEventArgs { TaskCompletionSource = tcs, MessageId = messageId });

            return tcs.Task;
        }

        public void Register<T>(Action<T> callback)
        {
            var page = callback.Target as Page;
            if (page != null)
            {
                Register(callback, page);
                return;
            }

            if (AsyncControllerType.IsInstanceOfType(callback.Target))
            {
                Register(callback, callback.Target);
                return;
            }

            var context = SynchronizationContext.Current;
            Register(callback, context);
        }

        public void Register<T>(Action<T> callback, object synchronizer)
        {
            if (!typeof(T).IsEnum && typeof(T) != typeof(int))
                throw new InvalidOperationException("Can only support registering callbacks for integer or enum types. The given type is: " + typeof(T).FullName);

            if (HttpContext.Current != null && synchronizer == null)
                throw new ArgumentNullException("synchronizer", "NServiceBus has detected that you're running in a web context but have passed in a null synchronizer. Please pass in a reference to a System.Web.UI.Page or a System.Web.Mvc.AsyncController.");

            if (synchronizer == null)
            {
                InternalRegister(GetCallbackInvocationActionFrom(callback), null);
                return;
            }

            var page = synchronizer as Page;
            if (page != null)
            {
                page.RegisterAsyncTask(new PageAsyncTask(
                    (sender, e, cb, extraData) => InternalRegister(cb, extraData),
                    new EndEventHandler(GetCallbackInvocationActionFrom(callback)),
                    null,
                    null
                ));
                return;
            }

            if (AsyncControllerType.IsInstanceOfType(synchronizer))
            {
                dynamic asyncController = synchronizer;
                asyncController.AsyncManager.OutstandingOperations.Increment();

                Register(GetMvcCallbackInvocationActionFrom(callback, asyncController.AsyncManager), null);

                return;
            }

            var synchronizationContext = synchronizer as SynchronizationContext;
            if (synchronizationContext != null)
            {
                InternalRegister(
                     ar => synchronizationContext.Post(
                         x => GetCallbackInvocationActionFrom(callback).Invoke(ar), null),
                     null
                     );
            }
        }

        static AsyncCallback GetMvcCallbackInvocationActionFrom<T>(Action<T> callback, dynamic am)
        {
            return asyncResult =>
            {
                HandleAsyncResult(callback, asyncResult);
                am.OutstandingOperations.Decrement();
            };
        }

        static AsyncCallback GetCallbackInvocationActionFrom<T>(Action<T> callback)
        {
            return asyncResult => HandleAsyncResult(callback, asyncResult);
        }

        static void HandleAsyncResult<T>(Action<T> callback, IAsyncResult asyncResult)
        {
            var cr = asyncResult.AsyncState as CompletionResult;
            if (cr == null) return;

            var action = callback as Action<int>;
            if (action != null)
            {
                action(cr.ErrorCode);
            }
            else
            {
                callback((T)Enum.ToObject(typeof(T), cr.ErrorCode));
            }
        }
    }
}