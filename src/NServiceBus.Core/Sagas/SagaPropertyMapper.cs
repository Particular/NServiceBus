namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// A helper class that proved syntactical sugar as part of <see cref="Saga.ConfigureHowToFindSaga" />.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
    public class SagaPropertyMapper<TSagaData> where TSagaData : class, IContainSagaData
    {
        internal SagaPropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
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
        public ToSagaExpression<TSagaData, TMessage> ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty)
        {
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            return new ToSagaExpression<TSagaData, TMessage>(sagaMessageFindingConfiguration, messageProperty);
        }

        /// <summary>
        /// Specify how to map between <typeparamref name="TSagaData"/> and <typeparamref name="TMessage"/> using a message header.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="headerName">The name of the header that contains the correlation value.</param>
        /// <returns>
        /// A <see cref="IToSagaExpression{TSagaData}" /> that provides the fluent chained
        /// <see cref="IToSagaExpression{TSagaData}.ToSaga" /> to link <typeparamref name="TMessage"/> with
        /// <typeparamref name="TSagaData"/> using <paramref name="headerName"/>.
        /// </returns>
        public IToSagaExpression<TSagaData> ConfigureHeaderMapping<TMessage>(string headerName)
        {
            Guard.AgainstNull(nameof(headerName), headerName);

            if(!(sagaMessageFindingConfiguration is IConfigureHowToFindSagaWithMessageHeaders sagaHeaderFindingConfiguration))
            {
                throw new Exception($"Unable to configure header mapping. To fix this, ensure that {sagaMessageFindingConfiguration.GetType().FullName} implements {nameof(IConfigureHowToFindSagaWithMessageHeaders)}.");
            }

            return new MessageHeaderToSagaExpression<TSagaData, TMessage>(sagaHeaderFindingConfiguration, headerName);
        }

        /// <summary>
        /// Specify the correlation property for instances of <typeparamref name="TSagaData"/>.
        /// </summary>
        /// <param name="sagaProperty">The correlation property to use when finding a saga instance.</param>
        /// <returns>
        /// A <see cref="CorrelatedSagaPropertyMapper{TSagaData}"/> that provides the fluent chained
        /// <see cref="CorrelatedSagaPropertyMapper{TSagaData}.ToMessage{TMessage}"/> to map a message type to
        /// the correlation property.
        /// </returns>
        public CorrelatedSagaPropertyMapper<TSagaData> MapSaga(Expression<Func<TSagaData, object>> sagaProperty)
        {
            Guard.AgainstNull(nameof(sagaProperty), sagaProperty);
            return new CorrelatedSagaPropertyMapper<TSagaData>(this, sagaProperty);
        }

        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
    }
}