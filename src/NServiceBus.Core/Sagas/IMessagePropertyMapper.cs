namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// A helper class that proved syntactical sugar as part of <see cref="SagaV2{TSagaData}.ConfigureHowToFindSaga(IConfigureHowToFindSagaWithMessage)" />.
    /// </summary>
    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Specify how to map between <see cref="SagaV2{TSagaData}.CorrelationPropertyName"/> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}" /> that represents the message.</param>
        void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}