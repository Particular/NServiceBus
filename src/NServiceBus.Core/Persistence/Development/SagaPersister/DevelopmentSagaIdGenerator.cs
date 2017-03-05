namespace NServiceBus.Features
{
    using System;
    using NServiceBus.Sagas;

    class DevelopmentSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(SagaIdGeneratorContext context)
        {
            if (context.CorrelationProperty == SagaCorrelationProperty.None)
            {
                throw new NotSupportedException("The development saga persister doesn't support custom saga finders.");
            }

            //here we assume single sagas since v6 doesn't allow more than one corr prop
            // note that we still have to use a GUID since moving to a string id will have to wait since its a breaking change
            return DeterministicGuid.Create(context.CorrelationProperty.Value);
        }
    }
}