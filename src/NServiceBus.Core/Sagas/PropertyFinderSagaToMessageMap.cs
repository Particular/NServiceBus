namespace NServiceBus
{
    using Sagas;
    using System;
    using System.Collections.Generic;

    class PropertyFinderSagaToMessageMap : CorrelationSagaToMessageMap
    {
        public Func<object, object> MessageProp;

        public override SagaFinderDefinition CreateSagaFinderDefinition(Type sagaEntityType)
        {
            return new SagaFinderDefinition(
                typeof(PropertySagaFinder<>).MakeGenericType(sagaEntityType),
                MessageType,
                new Dictionary<string, object>
                {
                    {"property-accessor", MessageProp},
                    {"saga-property-name", SagaPropName}
                });
        }
    }
}