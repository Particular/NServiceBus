namespace NServiceBus
{
    using Sagas;
    using System;
    using System.Collections.Generic;

    class PropertyFinderSagaToMessageMap : SagaToMessageMap
    {
        public Func<object, object> MessageProp;
        public string SagaPropName;
        public Type SagaPropType;

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