namespace NServiceBus
{
    using System;
    using Extensibility;
    using Sagas;

    class DefaultSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(string propertyName, object propertyValue, SagaMetadata metadata, ContextBag context)
        {
            // intentionally ignore the property name and the value.
            return CombGuid.Generate();
        }
    }
}