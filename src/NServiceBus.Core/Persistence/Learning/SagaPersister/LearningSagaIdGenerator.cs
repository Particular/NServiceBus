namespace NServiceBus
{
    using System;
    using Sagas;

    class LearningSagaIdGenerator : ISagaIdGenerator
    {
        public Guid Generate(SagaIdGeneratorContext context)
        {
            if (context.CorrelationProperty == SagaCorrelationProperty.None)
            {
                throw new NotSupportedException("The learning saga persister doesn't support custom saga finders.");
            }

            return Generate(context.SagaMetadata.SagaEntityType, context.CorrelationProperty.Name, context.CorrelationProperty.Value);
        }

        public static Guid Generate(Type sagaEntityType, string correlationPropertyName, object correlationPropertyValue)
        {
            // assumes single correlated sagas since v6 doesn't allow more than one corr prop
            // will still have to use a GUID since moving to a string id will have to wait since its a breaking change
            return DeterministicGuid.Create($"{sagaEntityType}_{correlationPropertyName}_{correlationPropertyValue}");
        }
    }
}