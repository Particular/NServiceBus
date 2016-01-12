namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// A helper class that proved syntactical sugar as part of <see cref="Saga.ConfigureHowToFindSaga"/>.
    /// </summary>
    /// <typeparam name="TSagaData">A type that implements <see cref="IContainSagaData"/>.</typeparam>
    public class SagaPropertyMapper<TSagaData> where TSagaData : IContainSagaData
    {
        IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration;

        internal SagaPropertyMapper(IConfigureHowToFindSagaWithMessage sagaMessageFindingConfiguration)
        {
            this.sagaMessageFindingConfiguration = sagaMessageFindingConfiguration;
        }

        /// <summary>
        /// Specify how to map between <typeparamref name="TSagaData"/> and <typeparamref name="TMessage"/> with identifier type <typeparamref name="TSagaIdentifier"/>.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <typeparam name="TSagaIdentifier">The type used to identify the saga.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}"/> that represents the message.</param>
        /// <returns>A <see cref="ToSagaExpression{TSagaData,TMessage,TSagaIdentifier}"/> that provides the fluent chained <see cref="ToSagaExpression{TSagaData,TMessage,TSagaIdentifier}.ToSaga"/> to link <paramref name="messageProperty"/> with <typeparamref name="TSagaData"/>.</returns>
        public ToSagaExpression<TSagaData, TMessage, TSagaIdentifier> ConfigureMapping<TMessage, TSagaIdentifier>(Expression<Func<TMessage, TSagaIdentifier>> messageProperty)
        {
            Guard.AgainstNull(nameof(messageProperty), messageProperty);
            return new ToSagaExpression<TSagaData, TMessage, TSagaIdentifier>(sagaMessageFindingConfiguration, messageProperty);
        }
    }
}