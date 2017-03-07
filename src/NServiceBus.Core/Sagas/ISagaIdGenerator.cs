namespace NServiceBus.Sagas
{
    using System;

    /// <summary>
    /// Saga id generator.
    /// </summary>
    public interface ISagaIdGenerator
    {
        /// <summary>
        /// Allows custom saga ID's to be generated.
        /// </summary>
        /// <param name="context">Context for the id generation.</param>
        /// <returns>The saga id to use for the given saga.</returns>
        Guid Generate(SagaIdGeneratorContext context);
    }
}