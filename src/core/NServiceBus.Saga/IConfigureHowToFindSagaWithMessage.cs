using System;
using System.Linq.Expressions;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Implementation provided by the infrastructure - don't implement this
    /// or register implementations of it in the container unless you intend
    /// to substantially change the way sagas work in nServiceBus.
    /// </summary>
    public interface IConfigureHowToFindSagaWithMessage
    {
        /// <summary>
        /// Specify that when the infrastructure is handling a message 
        /// of the given type, which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TSagaEntity"></typeparam>
        /// <param name="sagaEntityProperty"></param>
        /// <param name="messageProperty"></param>
        void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty) where TSagaEntity : ISagaEntity where TMessage : IMessage;
    }
}