namespace NServiceBus
{
    using System;

    public partial class Saga
    {
        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <typeparamref name="TTimeoutMessageType"/>.</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(DateTime at)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at) where TTimeoutMessageType : new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send call <paramref name="action"/>.</param>
        /// <param name="action">Callback to execute after <paramref name="at"/> is reached.</param>
        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(DateTime, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, Action<TTimeoutMessageType> action) where TTimeoutMessageType : new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request for a timeout to occur at the given <see cref="DateTime"/>.
        /// </summary>
        /// <param name="at"><see cref="DateTime"/> to send timeout <paramref name="timeoutMessage"/>.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="at"/> is reached.</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)")]
        protected void RequestTimeout<TTimeoutMessageType>(DateTime at, TTimeoutMessageType timeoutMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(TimeSpan within)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within) where TTimeoutMessageType : new()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Request for a timeout to occur within the give <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message that is sent after <paramref name="within"/> expires.</param>
        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "Saga.RequestTimeout<TTimeoutMessageType>(TimeSpan, TTimeoutMessageType)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, Action<TTimeoutMessageType> messageConstructor) where TTimeoutMessageType : new()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request for a timeout to occur within the given <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="within">Given <see cref="TimeSpan"/> to delay timeout message by.</param>
        /// <param name="timeoutMessage">The message to send after <paramref name="within"/> expires.</param>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "RequestTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)")]
        protected void RequestTimeout<TTimeoutMessageType>(TimeSpan within, TTimeoutMessageType timeoutMessage)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the <paramref name="message"/> using the bus to the endpoint that caused this saga to start.
        /// </summary>
        [ObsoleteEx(
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ReplyToOriginator(object message)")]
        protected void ReplyToOriginator(object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of the given type, setting its properties using the given action,
        /// and sends it using the bus to the endpoint that caused this saga to start.
        /// </summary>
        /// <typeparam name="TMessage">The type of message to construct.</typeparam>
        /// <param name="messageConstructor">An <see cref="Action"/> which initializes properties of the message reply with.</param>
        [ObsoleteEx(
            Message = "Construct your message and pass it to the non Action overload.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0",
            ReplacementTypeOrMember = "ReplyToOriginator(object message)")]
        protected virtual void ReplyToOriginator<TMessage>(Action<TMessage> messageConstructor) where TMessage : new()
        {
            throw new NotImplementedException();
        }
    }
}