namespace NServiceBus.Sagas
{
    using System;

    /// <summary>
    /// Saga id generator.
    /// </summary>
    public interface ISagaIdGenerator
    {
        /// <summary>
        /// Generates a saga id based on property name and property value.
        /// </summary>
        /// <param name="context">Context for the id generation.</param>
        /// <returns>The saga id to use.</returns>
        Guid Generate(SagaIdGeneratorContext context);
    }
}