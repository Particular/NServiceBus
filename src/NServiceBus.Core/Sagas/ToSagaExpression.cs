namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Allows a more fluent way to map sagas.
    /// </summary>
    public class ToSagaExpression<TSagaData, TMessage> where TSagaData : IContainSagaData
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ToSagaExpression{TSagaData,TMessage}" />.
        /// </summary>
        public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TMessage, object>> messageProperty)
        {
            Guard.AgainstNull(nameof(sagaMessageFindingConfiguration), sagaMessageFindingConfiguration);
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.messageProperty = messageProperty;
        }


        /// <summary>
        /// Defines the property on the saga data to which the message property should be mapped.
        /// </summary>
        /// <param name="sagaEntityProperty">The property to map.</param>
        public void ToSaga(Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            Guard.AgainstNull(nameof(sagaEntityProperty), sagaEntityProperty);
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }

        Expression<Func<TMessage, object>> messageProperty;
        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
    }
}