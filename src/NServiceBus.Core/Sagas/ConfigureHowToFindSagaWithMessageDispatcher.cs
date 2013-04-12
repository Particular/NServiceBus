namespace NServiceBus.Sagas
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using NServiceBus.Saga;
    using NServiceBus.Utils.Reflection;

    /// <summary>
    /// Class used to bridge the dependency between Saga{T} in NServiceBus.dll and
    /// the Configure class found in this project in NServiceBus.Core.dll.
    /// </summary>
    public class ConfigureHowToFindSagaWithMessageDispatcher : IConfigureHowToFindSagaWithMessage
    {
        void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty, true);
            var messageProp = Reflect<TMessage>.GetProperty(messageProperty, false);

            ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);
            ThrowIfNotPropertyLambdaExpression(messageProperty, messageProp);

            Features.Sagas.ConfigureHowToFindSagaWithMessage(typeof(TSagaEntity), sagaProp, typeof(TMessage), messageProp);
        }

        private static void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression, PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
            {
                throw new ArgumentException(
                    String.Format(
                        "Only public properties are supported for mapping Sagas. The lambda expression provided '{0}' is not mapping to a Property!",
                        expression.Body));
            }
        }
    }
}