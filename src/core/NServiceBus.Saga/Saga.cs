using System;
using System.Linq.Expressions;

namespace NServiceBus.Saga
{
    /// <summary>
    /// This class is used to define sagas containing data and handling a message.
    /// To handle more message types, implement <see cref="IMessageHandler{T}"/>
    /// for the relevant types.
    /// To signify that the receipt of a message should start this saga,
    /// implement <see cref="ISagaStartedBy{T}"/> for the relevant message type.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="ISagaEntity"/>.</typeparam>
    public abstract class Saga<T> : ISaga<T> where T : ISagaEntity
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

        internal bool configuring;
        void ISaga.Configure()
        {
            configuring = true;
            ConfigureHowToFindSaga();
            configuring = false;
        }

        /// <summary>
        /// Override this method in order to call ConfigureMapping.
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
        protected void ConfigureMapping<TMessage>(Expression<Func<T, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TMessage : IMessage
        {
            if (!configuring)
                throw new InvalidOperationException("Cannot configure mappings outside of 'ConfigureHowToFindSaga'.");

            Dispatcher.ConfigureHowToFindSagaWithMessage(sagaEntityProperty, messageProperty);
        }

        /// <summary>
        /// Bus object used for retrieving the sender endpoint which caused this saga to start.
        /// Necessary for <see cref="ReplyToOriginator" />.
        /// </summary>
        public IBus Bus { get; set; }

        /// <summary>
        /// Indicates that the saga is complete.
        /// In order to set this value, use the <see cref="MarkAsComplete" /> method.
        /// </summary>
        public bool Completed { get; private set; }

        /// <summary>
        /// Request for a timeout to occur at the given time.
        /// Causes a callback to the <see cref="Timeout" /> method with the given state.
        /// </summary>
        /// <param name="at"></param>
        /// <param name="withState"></param>
        protected void RequestTimeout(DateTime at, object withState)
        {
            RequestTimeout(at - DateTime.Now, withState);
        }

        /// <summary>
        /// Request for a timeout to occur at the given time.
        /// Causes a callback to the <see cref="Timeout" /> method with the given state.
        /// </summary>
        /// <param name="within"></param>
        /// <param name="withState"></param>
        protected void RequestTimeout(TimeSpan within, object withState)
        {
            if (within <= TimeSpan.Zero)
                Timeout(withState);
            else
                Bus.Send(new TimeoutMessage(within, Data, withState));
        }

        /// <summary>
        /// Sends the given messages using the bus to the endpoint that caused this saga to start.
        /// </summary>
        /// <param name="messages"></param>
        protected void ReplyToOriginator(params IMessage[] messages)
        {
            if (string.IsNullOrEmpty(Data.Originator))
                Dispatcher.TriedToReplyToNullOriginator();
            else
                Bus.Send(Data.Originator, Data.OriginalMessageId, messages);
        }

        /// <summary>
        /// Instantiates a message of the given type, setting its properties using the given action,
        /// and sends it using the bus to the endpoint that caused this saga to start.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="messageConstructor"></param>
        protected void ReplyToOriginator<TMessage>(Action<TMessage> messageConstructor) where TMessage : IMessage
        {
            var message = Bus.CreateInstance(messageConstructor);
            ReplyToOriginator(message);
        }

        /// <summary>
        /// Marks the saga as complete.
        /// This may result in the sagas state being deleted by the persister.
        /// </summary>
        protected void MarkAsComplete()
        {
            Completed = true;
        }

        /// <summary>
        /// Notifies that the timeout it previously requested occurred.
        /// </summary>
        /// <param name="state">The object passed as the "withState" parameter to RequestTimeout.</param>
        public abstract void Timeout(object state);
    }
}
