using System;
using System.Linq.Expressions;

namespace NServiceBus.Saga
{
    /// <summary>
    /// Allows the user to control how to lookup sagas given message properties
    /// </summary>
    public interface IConfigureHowToFindSagaWithMessage
    {
        /// <summary>
        /// When the infrastructure is handling a message of the given type
        /// this specifies which message property should be matched to 
        /// which saga entity property in the persistent saga store.
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <typeparam name="TSagaEntity"></typeparam>
        /// <param name="sagaEntityProperty"></param>
        /// <param name="messageProperty"></param>
        void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty);
    }
}