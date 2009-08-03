using System;
using System.Linq.Expressions;
using System.Reflection;
using NServiceBus.Saga;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Sagas.Impl
{
    /// <summary>
    /// Double-dispatch class.
    /// </summary>
    public class ConfigureHowToFindSagaWithMessageDispatcher : IConfigureHowToFindSagaWithMessage
    {
        /// <summary>
        /// Callback for when saga wants to configure which property of which message type
        /// should be used to look it up based on its given property.
        /// </summary>
        internal Action<Type, PropertyInfo, Type, PropertyInfo> CallbackWithSagaAndMessageProperties { get; set; }

        

        /// <summary>
        /// Configures how to lockup a given saga when a message of type TMessage arrives
        /// </summary>
        /// <typeparam name="TSagaEntity"></typeparam>
        /// <typeparam name="TMessage"></typeparam>
        /// <param name="sagaEntityProperty"></param>
        /// <param name="messageProperty"></param>
        public void ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            if (CallbackWithSagaAndMessageProperties == null) return;

            var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty);
            var messageProp = Reflect<TMessage>.GetProperty(messageProperty);

            CallbackWithSagaAndMessageProperties(typeof(TSagaEntity), sagaProp, typeof(TMessage), messageProp);
        }
    }
}