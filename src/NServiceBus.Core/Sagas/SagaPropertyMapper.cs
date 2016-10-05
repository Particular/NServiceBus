namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// A helper class that proved syntactical sugar as part of <see cref="Saga.ConfigureHowToFindSaga" />.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData" />.</typeparam>
    public class SagaPropertyMapper<TSagaData> where TSagaData : IContainSagaData
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

        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;
    }
}