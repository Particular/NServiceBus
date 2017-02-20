namespace NServiceBus.Features
{
    using System;
    using Extensibility;
    using NServiceBus.Sagas;

    class DevelopmentSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(string propertyName, object propertyValue, SagaMetadata metadata, ContextBag context)
        {
            //here we assume single sagas since v6 doesn't allow more than one corr prop
            // note that we still have to use a GUID since moving to a string id will have to wait since its a breaking change
            return DeterministicGuid.Create(propertyValue);
        }
    }
}