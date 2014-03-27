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
        void IConfigureHowToFindSagaWithMessage.ConfigureMapping<TSagaEntity, TMessage>(Expression<Func<TSagaEntity, object>> sagaEntityProperty, Func<TMessage, object> messageFunc)
        {
            var sagaProp = Reflect<TSagaEntity>.GetProperty(sagaEntityProperty, true);

            ThrowIfNotPropertyLambdaExpression(sagaEntityProperty, sagaProp);

            Func<object, object> genericMessageFunc = message => messageFunc((TMessage)message);

            Features.Sagas.ConfigureHowToFindSagaWithMessage(typeof(TSagaEntity), sagaProp, typeof(TMessage), genericMessageFunc);
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