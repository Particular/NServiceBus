using System;
using NServiceBus;

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
        public T Data { get; set; }

        public ISagaEntity Entity
        {
            get { return Data; }
            set { Data = (T)value; }
        }

        public IBus Bus { get; set; }

        public bool Completed
        {
            get { return completed; }
        }

        protected void RequestTimeout(DateTime at, object withState)
        {
            RequestTimeout(at - DateTime.Now, withState);
        }

        protected void RequestTimeout(TimeSpan at, object withState)
        {
            if (at <= TimeSpan.Zero)
                this.Timeout(withState);
            else
                Bus.Send(new TimeoutMessage(at, Data, withState));
        }

        protected void ReplyToOriginator(params IMessage[] messages)
        {
            Bus.Send(Data.Originator, messages);
        }

        protected void ReplyToOriginator<K>(Action<K> messageConstructor) where K : IMessage
        {
            Bus.Send<K>(Data.Originator, messageConstructor);
        }

        protected void MarkAsComplete()
        {
            this.completed = true;
        }

        private bool completed;

        /// <summary>
        /// Notifies that the timeout it previously requested occurred.
        /// </summary>
        /// <param name="state">The object passed as the "withState" parameter to RequestTimeout.</param>
        public abstract void Timeout(object state);
    }
}
