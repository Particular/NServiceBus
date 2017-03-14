namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;
    using Sagas;

    /// <summary>
    /// A helper class that proved syntactical sugar as part of <see cref="SimpleSaga{TSagaData}.ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage)" />.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
    public class MessagePropertyMapper<TSagaData> 
        where TSagaData : IContainSagaData
    {
        internal MessagePropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration, Expression<Func<TSagaData, object>> sagaEntityProperty)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
            this.sagaEntityProperty = sagaEntityProperty;
        }

        /// <summary>
        /// Specify how to map between <typeparamref name="TSagaData" /> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}" /> that represents the message.</param>
        /// <returns>
        /// A <see cref="ToSagaExpression{TSagaData,TMessage}" /> that provides the fluent chained
        /// <see cref="ToSagaExpression{TSagaData,TMessage}.ToSaga" /> to link <paramref name="messageProperty" /> with
        /// <typeparamref name="TSagaData" />.
        /// </returns>
        public void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            if (sagaEntityProperty == null)
            {
                throw new Exception($"No CorrelationProperty has been defined by the saga that uses the saga data \'{nameof(TSagaData)}\' hence it is exprected that an a {nameof(IFindSagas<TSagaData>)} will be defined for all messages the saga handles.");
            }
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            sagaMessageFindingConfiguration.ConfigureMapping(sagaEntityProperty, messageProperty);
        }

        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
        Expression<Func<TSagaData, object>> sagaEntityProperty;
    }
}