namespace NServiceBus.Saga
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Allows a more fluent way to map sagas
    /// </summary>
    public class ToSagaExpression<TSaga,TMessage> where TSaga : IContainSagaData
    {
        readonly IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        readonly Expression<Func<TMessage, object>> messageProperty;

        /// <summary>
        /// Constructs the expression
        /// </summary>
        public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TMessage, object>> messageProperty)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.messageProperty = messageProperty;
        }


        /// <summary>
        /// Defines the property on the saga data to which the message property should be mapped
        /// </summary>
        /// <param name="sagaEntityProperty">The property to map</param>
        public void ToSaga(Expression<Func<TSaga, object>> sagaEntityProperty)
        {
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }
    }
}