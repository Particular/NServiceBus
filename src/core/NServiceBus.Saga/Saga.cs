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
        private T data;

        public T Data
        {
            get { return data; }
            set { data = value; }
        }

        public ISagaEntity Entity
        {
            get { return data; }
            set { data = (T)value; }
        }

        private IBus bus;

        public IBus Bus
        {
            get { return bus; }
            set { bus = value; }
        }

        public bool Completed
        {
            get { return completed; }
        }

        protected void RequestTimeout(TimeSpan at, object withState)
        {
            bus.Send(new TimeoutMessage(at, data, withState));
        }

        protected void RequestTimeout(DateTime at, object withState)
        {
            bus.Send(new TimeoutMessage(at - DateTime.Now, data, withState));
        }

        protected void ReplyToOriginator(params IMessage[] messages)
        {
            bus.Send(data.Originator, messages);
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
