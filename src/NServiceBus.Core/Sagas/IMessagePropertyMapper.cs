namespace NServiceBus
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Maps message properties to the saga correlation property.
    /// </summary>
    public interface IMessagePropertyMapper
    {
        /// <summary>
        /// Configures the mapping between <see cref="SagaV2{TSagaData}.CorrelationPropertyName"/> and <typeparamref name="TMessage" />.
        /// </summary>
        /// <typeparam name="TMessage">The message type to map to.</typeparam>
        /// <param name="messageProperty">An <see cref="Expression{TDelegate}" /> that represents the message.</param>
        void ConfigureMapping<TMessage>(Expression<Func<TMessage, object>> messageProperty);
    }
}