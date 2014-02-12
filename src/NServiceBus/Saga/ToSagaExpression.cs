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
        readonly Func<TMessage, object> messageFunc;

        /// <summary>
        /// Constructs the expression
        /// </summary>
        public ToSagaExpression(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Func<TMessage, object> messageFunc)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.messageFunc = messageFunc;
        }


        /// <summary>
        /// Defines the property on the saga data to which the message property should be mapped
        /// </summary>
        /// <param name="sagaEntityProperty">The property to map</param>
        public void ToSaga(Expression<Func<TSaga, object>> sagaEntityProperty)
        {
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageFunc);
        }
    }
}