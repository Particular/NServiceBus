namespace NServiceBus.Sagas
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;
    using Saga;
    using Utils.Reflection;

    /// <summary>
    /// Class used to bridge the dependency between <see cref="Saga{T}"/> and <see cref="Configure"/>.
    /// </summary>
    public class ConfigureHowToFindSagaWithMessageDispatcher : IConfigureHowToFindSagaWithMessage
    {
        void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Expression<Func<TMessage, object>> messageProperty)
        {
            var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty, true);
            var messageProp = Reflect<TMessage>.GetProperty(messageProperty, true);

            ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);
            ThrowIfNotPropertyLambdaExpression(messageProperty, messageProp);

            Features.Sagas.ConfigureHowToFindSagaWithMessage(typeof(TSagaEntity), sagaProp, typeof(TMessage), messageProp);
        }

        void ThrowIfNotPropertyLambdaExpression<TSagaEntity>(Expression<Func<TSagaEntity, object>> expression, PropertyInfo propertyInfo)
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