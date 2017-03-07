namespace NServiceBus
{
    using System;
    using Sagas;

    class DefaultSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(SagaIdGeneratorContext context)
        {
            // intentionally ignore the property name and the value.
            return CombGuid.Generate();
        }
    }
}