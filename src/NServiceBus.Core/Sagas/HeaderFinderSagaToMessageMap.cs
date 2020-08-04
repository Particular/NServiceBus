namespace NServiceBus
{
    using Sagas;
    using System;
    using System.Collections.Generic;

    class HeaderFinderSagaToMessageMap : CorrelationSagaToMessageMap
    {
        public string HeaderName;

        public override SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType)
        {
            return new SagaFinderDefinition(
                typeof(HeaderPropertySagaFinder<>).MakeGenericType(sagaEntityType),
                MessageType,
                new Dictionary<string, object>
                {
                    {"message-header-name", HeaderName},
                    {"saga-property-name", SagaPropName},
                    {"saga-property-type", SagaPropType}
                });
        }
    }
}