namespace NServiceBus.Saga
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// This class is used to define sagas containing data and handling a message.
    /// To handle more message types, implement <see cref="IHandleMessages{T}"/>
    /// for the relevant types.
    /// To signify that the receipt of a message should start this saga,
    /// implement <see cref="IAmStartedByMessages{T}"/> for the relevant message type.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IContainSagaData"/>.</typeparam>
    public abstract class
        Saga<T> : IConfigurable, ISaga<T> where T : IContainSagaData
    {
        /// <summary>
        /// The saga's strongly typed data.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// A more generic projection on <see cref="Data" />.
        /// </summary>
        public IContainSagaData Entity
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
        /// Call <see cref="ConfigureMapping{TMessage}(System.Linq.Expressions.Expression{System.Func{T,object}},System.Linq.Expressions.Expression{System.Func{TMessage,object}})"/> for each property of each message you want
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
        [ObsoleteEx(Message = "Use the more explicit ConfigureMapping<T>.ToSaga<TSaga>(...) instead. For example 'ConfigureMapping<MyMessage>(message => message.MyProp).ToSaga(sagaData => sagaData.MyProp);'.", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected virtual void ConfigureMapping<TMessage>(Expression<Func<T, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            if (!configuring)
                throw new InvalidOperationException("Cannot configure mappings outside of 'ConfigureHowToFindSaga'.");

            SagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }

        /// <summary>
        /// When the infrastructure is handling a message of the given type
        /// this specifies which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        protected virtual ToSagaExpression<T, TMessage> ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (!configuring)
                throw new InvalidOperationException("Cannot configure mappings outside of 'ConfigureHowToFindSaga'.");

            return new ToSagaExpression<T, TMessage>(SagaMessageFindingConfiguration, messageProperty);
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
                    throw new InvalidOperationException("No IBus instance available, please configure one and also verify that you're not defining your own Bus property in your saga since that hides the one in the base class");

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
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <typeparamref name="TTimeoutMessageType"/>.</param>
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at)
        {
            RequestUtcTimeout(at, Bus.CreateInstance<TTimeoutMessageType>());
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send call <paramref name="action"/>.</param>
        /// <param name="action">Callback to execute after <paramref name="at"/> is reached.</param>
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, Action<TTimeoutMessageType> action)
        {
            RequestUtcTimeout(at, Bus.CreateInstance(action));
        }


        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <paramref name="timeoutMessage"/>.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at"/> is reached.</param>
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)
        {
            if (at.Kind == DateTimeKind.Unspecified)
                throw new InvalidOperationException("Kind property of DateTime 'at' must be specified.");

            VerifySagaCanHandleTimeout(timeoutMessage);
            SetTimeoutHeaders(timeoutMessage);

            Bus.Defer(at, timeoutMessage);
        }

        void VerifySagaCanHandleTimeout<TTimeoutMessageType>(TTimeoutMessageType timeoutMessage)
        {
            var canHandleTimeoutMessage = this is IHandleTimeouts<TTimeoutMessageType>;
            if (!canHandleTimeoutMessage)
            {
                var message = string.Format("The type '{0}' cannot request timeouts for '{1}' because it does not implement 'IHandleTimeouts<{2}>'", GetType().Name, timeoutMessage, typeof(TTimeoutMessageType).Name);
                throw new Exception(message);
            }
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within)
        {
            RequestUtcTimeout(within, Bus.CreateInstance<TTimeoutMessageType>());
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message that is sent after <paramref name="within"/> expires.</param>
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, Action<TTimeoutMessageType> messageConstructor)
        {
            RequestUtcTimeout(within, Bus.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Request for a timeout to occur within the given <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within"/> expires.</param>
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            VerifySagaCanHandleTimeout(timeoutMessage);
            SetTimeoutHeaders(timeoutMessage);

            Bus.Defer(within, timeoutMessage);
        }

        #region Obsoleted RequestUtcTimeout
        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <typeparamref name="TTimeoutMessageType"/>.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(DateTime at)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(DateTime at)
        {
            RequestTimeout(at, Bus.CreateInstance<TTimeoutMessageType>());
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send the message produced by <paramref name="messageConstructor"/>.</param>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message that is sent when <paramref name="at"/> is reached.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(DateTime at, Action<TTimeoutMessageType> messageConstructor)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(DateTime at, Action<TTimeoutMessageType> messageConstructor)
        {
            RequestTimeout(at, Bus.CreateInstance(messageConstructor));
        }


        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <paramref name="timeoutMessage"/>.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at"/> is reached.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)
        {
            RequestTimeout(at, timeoutMessage);
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(TimeSpan within)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(TimeSpan within)
        {
            RequestTimeout(within, Bus.CreateInstance<TTimeoutMessageType>());
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message that is sent after <paramref name="within"/> expires.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(TimeSpan within, Action<TTimeoutMessageType> action)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(TimeSpan within, Action<TTimeoutMessageType> messageConstructor)
        {
            RequestTimeout(within, Bus.CreateInstance(messageConstructor));
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay <paramref name="timeoutMessage"/> by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within"/> expires.</param>
        [ObsoleteEx(Replacement = "RequestTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)", TreatAsErrorFromVersion = "5.0", RemoveInVersion = "6.0")]
        protected void RequestUtcTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            RequestTimeout(within, timeoutMessage);
        }
        #endregion

        private void SetTimeoutHeaders(object toSend)
        {

            Headers.SetMessageHeader(toSend, Headers.SagaId, Data.Id.ToString());
            Headers.SetMessageHeader(toSend, Headers.IsSagaTimeoutMessage, Boolean.TrueString);
            Headers.SetMessageHeader(toSend, Headers.SagaType, GetType().AssemblyQualifiedName);
        }

        /// <summary>
        /// Sends the <paramref name="messages"/> using the bus to the endpoint that caused this saga to start.
        /// </summary>
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
        /// <typeparam name="TMessage">The type of message to construct.</typeparam>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message reply with.</param>
        protected virtual void ReplyToOriginator<TMessage>(Action<TMessage> messageConstructor)
        {
            if (messageConstructor != null)
                ReplyToOriginator(Bus.CreateInstance(messageConstructor));
            else
                ReplyToOriginator(null);
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
        [ObsoleteEx(Message = "2.6 style timeouts has been replaced. Please implement IHandleTimeouts<T> instead.", TreatAsErrorFromVersion = "4.0", RemoveInVersion = "5.0")]
        public virtual void Timeout(object state)
        {
        }

    }
}
