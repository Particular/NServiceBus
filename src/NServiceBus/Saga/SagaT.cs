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
    public abstract class Saga<T> : Saga where T : IContainSagaData, new()
    {
        /// <summary>
        /// The saga's strongly typed data. Wraps <see cref="Saga.Entity"/>.
        /// </summary>
        public T Data
        {
            get { return (T) Entity; }
            set { Entity = value; }
        }

        /// <summary>
        /// When the infrastructure is handling a message of the given type
        /// this specifies which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        protected virtual ToSagaExpression<T, TMessage> ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            return base.ConfigureMapping<T, TMessage>(messageProperty);
        }
    }
}
