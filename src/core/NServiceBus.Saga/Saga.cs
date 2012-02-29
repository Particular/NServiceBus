using System;
using System.Linq.Expressions;

namespace NServiceBus.Saga
{
    using System.Linq;

    /// <summary>
    /// This class is used to define sagas containing data and handling a message.
    /// To handle more message types, implement <see cref="IMessageHandler{T}"/>
    /// for the relevant types.
    /// To signify that the receipt of a message should start this saga,
    /// implement <see cref="ISagaStartedBy{T}"/> for the relevant message type.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="ISagaEntity"/>.</typeparam>
    public abstract class Saga<T> : IConfigurable, ISaga<T>, IHandleMessages<TimeoutMessage>, IHandleMessages<ITimeoutState> where T : ISagaEntity
    {
        /// <summary>
        /// The saga's strongly typed data.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// A more generic projection on <see cref="Data" />.
        /// </summary>
        public ISagaEntity Entity
        {
            get { return Data; }
            set { Data = (T)value; }
        }

        private bool configuring;
        void IConfigurable.Configure()
        {
            configuring = true;
            ConfigureHowToFindSaga();
            configuring = false;
        }

        /// <summary>
        /// Override this method in order to configure how this saga's data should be found.
        /// Call ConfigureMapping&lt;TMessage&gt; for each property of each message you want
        /// to use for lookup.
        /// </summary>
        public virtual void ConfigureHowToFindSaga()
        {
        }

        /// <summary>
        /// When the infrastructure is handling a message of the given type
        /// this specifies which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="sagaEntityProperty"></param>
        /// <param name="messageProperty"></param>
        protected virtual void ConfigureMapping<TMessage>(Expression<Func<T, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            if (!configuring)
                throw new InvalidOperationException("Cannot configure mappings outside of 'ConfigureHowToFindSaga'.");

            SagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }


        /// <summary>
        /// Called by saga to notify the infrastructure when attempting to reply to message where the originator is null
        /// </summary>
        public IHandleReplyingToNullOriginator HandleReplyingToNullOriginator { get; set; }


        /// <summary>
        /// Bus object used for retrieving the sender endpoint which caused this saga to start.
        /// Necessary for <see cref="ReplyToOriginator" />.
        /// </summary>
        public IBus Bus
        {
            get
            {
                if (bus == null)
                    throw new InvalidOperationException("No IBus instance availble, please configure one and also verify that you're not defining your own Bus property in your saga since that hides the one in the base class");

                return bus;
            }

            set { bus = value; }
        }

        IBus bus;

        /// <summary>
        /// Object used to configure mapping between saga properties and message properties
        /// for the purposes of finding sagas when a message arrives.
        /// 
        /// Do NOT use at runtime (handling messages) - it will be null.
        /// </summary>
        public IConfigureHowToFindSagaWithMessage SagaMessageFindingConfiguration { get; set; }

        /// <summary>
        /// Indicates that the saga is complete.
        /// In order to set this value, use the <see cref="MarkAsComplete" /> method.
        /// </summary>
        public bool Completed { get; private set; }


        /// <summary>
        /// Request for a timeout to occur at the given time
        /// </summary>
        /// <param name="at"></param>
        protected void RequestUtcTimeout<TTimeoutmessageType>(DateTime at) where TTimeoutmessageType : new()
        {
            TTimeoutmessageType m = new TTimeoutmessageType();
            RequestUtcTimeout(at,m);
        }

        /// <summary>
        /// Request for a timeout to occur at the given time
        /// </summary>
        /// <param name="at"></param>
        /// <param name="timeoutMessage"></param>
        protected void RequestUtcTimeout<TTimeoutmessageType>(DateTime at, TTimeoutmessageType timeoutMessage)
        {
            if (at.Kind == DateTimeKind.Unspecified)
                throw new InvalidOperationException("Kind property of DateTime 'at' must be specified.");

            RequestUtcTimeout(at.ToUniversalTime() - DateTime.UtcNow, timeoutMessage);
        }

        /// <summary>
        /// Request for a timeout to occur within the give timespan
        /// </summary>
        /// <param name="within"></param>
        protected void RequestUtcTimeout<TTimeoutmessageType>(TimeSpan within) where TTimeoutmessageType : new()
        {
            var m = new TTimeoutmessageType();

            RequestUtcTimeout(within,m );
        }

        /// <summary>
        /// Request for a timeout to occur within the give timespan
        /// </summary>
        /// <param name="within"></param>
        /// <param name="timeoutMessage"></param>
        protected void RequestUtcTimeout<TTimeoutmessageType>(TimeSpan within, TTimeoutmessageType timeoutMessage)
        {
            object toSend = timeoutMessage;

            if (!typeof(TTimeoutmessageType).IsMessageType())
                toSend = new TimeoutMessage(within, Data, toSend);

            toSend.SetHeader(Headers.SagaId, Data.Id.ToString());

            if (within <= TimeSpan.Zero)
                Bus.SendLocal(toSend);
            else
                Bus.Defer(within, toSend);
        }

        /// <summary>
        /// Sends the given messages using the bus to the endpoint that caused this saga to start.
        /// </summary>
        /// <param name="messages"></param>
        protected virtual void ReplyToOriginator(params object[] messages)
        {
            if (string.IsNullOrEmpty(Data.Originator))
                HandleReplyingToNullOriginator.TriedToReplyToNullOriginator();
            else
                Bus.Send(Data.Originator, Data.OriginalMessageId, messages);
        }

        /// <summary>
        /// Instantiates a message of the given type, setting its properties using the given action,
        /// and sends it using the bus to the endpoint that caused this saga to start.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="messageConstructor"></param>
        protected virtual void ReplyToOriginator<TMessage>(Action<TMessage> messageConstructor)
        {
            var message = Bus.CreateInstance(messageConstructor);
            ReplyToOriginator(message);
        }

        /// <summary>
        /// Marks the saga as complete.
        /// This may result in the sagas state being deleted by the persister.
        /// </summary>
        protected virtual void MarkAsComplete()
        {
            Completed = true;
        }

        /// <summary>
        /// Notifies that the timeout it previously requested occurred.
        /// </summary>
        /// <param name="state">The object passed as the "withState" parameter to RequestTimeout.</param>
        [Obsolete("2.6 style timeouts has been replaced. Please implement IHandleTimeouts<T> instead",false)]
        public virtual void Timeout(object state)
        {
        }

        /// <summary>
        /// Message handler for Timeout Message 
        /// </summary>
        /// <param name="message">Timeout Message</param>
        public void Handle(TimeoutMessage message)
        {
            Timeout(message.State);
        }

        /// <summary>
        /// Message handler for Timeout State 
        /// </summary>
        /// <param name="message">Timeout State</param>
        public void Handle(ITimeoutState message)
        {
            var messageType = message.GetType();

            // search for the timeout method using reflection
            var methodInfo = GetType().GetMethod("Timeout", new[] { messageType });

            if (methodInfo == null || methodInfo.GetParameters().First().ParameterType != messageType)
                throw new InvalidOperationException(string.Format("Timeout arrived with state {0}, but no method with signature void Timeout({0}). Please implement IHandleTimeouts<{0}> on your {1} class", messageType, GetType()));

            methodInfo.Invoke(this, new[] { message });
        }
    }
}
