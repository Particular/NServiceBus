namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using Sagas;

    class DefaultSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(SagaIdGeneratorContext context)
        {
            return CombGuid.Generate();
        }
    }
}