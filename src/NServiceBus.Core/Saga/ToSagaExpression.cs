namespace NServiceBus.Saga
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Allows a more fluent way to map sagas
    /// </summary>
    public class ToSagaExpression<TSagaData, TMessage> where TSagaData : IContainSagaData
    {
        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        Expression<Func<TMessage, object>> messageProperty;

        /// <summary>
        /// Constructs the expression
        /// </summary>
        public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TMessage, object>> messageProperty)
        {
            Guard.AgainstNull(sagaMessageFindingConfiguration, "sagaMessageFindingConfiguration");
            Guard.AgainstNull(messageProperty, "messageProperty");
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.messageProperty = messageProperty;
        }


        /// <summary>
        /// Defines the property on the saga data to which the message property should be mapped
        /// </summary>
        /// <param name="sagaEntityProperty">The property to map</param>
        public void ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            Guard.AgainstNull(sagaEntityProperty, "sagaEntityProperty");
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }
    }
}