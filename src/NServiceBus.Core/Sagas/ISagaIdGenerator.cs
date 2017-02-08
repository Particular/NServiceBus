namespace NServiceBus.Sagas
{
    using System;
    using Extensibility;

    /// <summary>
    /// Saga id generator.
    /// </summary>
    public interface ISagaIdGenerator
    {
        /// <summary>
        /// Generates a saga id based on property name and property value.
        /// </summary>
        /// <param name="propertyName">The property name. Might be null when a custom finder is used.</param>
        /// <param name="propertyValue">The property value. Might be null when a custom finder is used.</param>
        /// <param name="metadata">The saga metadata.</param>
        /// <param name="context">The context bag.</param>
        /// <returns>The saga id.</returns>
        Guid Generate(string propertyName, object propertyValue, SagaMetadata metadata, ContextBag context);
    }
}