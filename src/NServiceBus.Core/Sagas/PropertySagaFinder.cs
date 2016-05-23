namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using ObjectBuilder;
    using Persistence;
    using Sagas;

    class PropertySagaFinder<TSagaData> : SagaFinder where TSagaData : class, IContainSagaData
    {
        public PropertySagaFinder(ISagaPersister sagaPersister)
        {
            this.sagaPersister = sagaPersister;
        }

        public override Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message)
        {
            var propertyAccessor = (Func<object, object>) finderDefinition.Properties["property-accessor"];
            var propertyValue = propertyAccessor(message);

            var sagaPropertyName = (string) finderDefinition.Properties["saga-property-name"];

            if (sagaPropertyName.ToLower() == "id")
            {
                return sagaPersister.Get<TSagaData>((Guid) propertyValue, storageSession, context) as Task<IContainSagaData>;
            }

            var lookupValues = context.GetOrCreate<SagaLookupValues>();

            lookupValues.Add<TSagaData>(sagaPropertyName, propertyValue);

            return sagaPersister.Get<TSagaData>(sagaPropertyName, propertyValue, storageSession, context) as Task<IContainSagaData>;
        }

        ISagaPersister sagaPersister;
    }


    class SagaLookupValues
    {
        public void Add<TSagaData>(string propertyName, object propertyValue)
        {
            entries[typeof(TSagaData)] = new LookupValue
            {
                PropertyName = propertyName,
                PropertyValue = propertyValue
            };
        }

        public bool TryGet(Type sagaType, out LookupValue value)
        {
            return entries.TryGetValue(sagaType, out value);
        }

        Dictionary<Type, LookupValue> entries = new Dictionary<Type, LookupValue>();

        public class LookupValue
        {
            public string PropertyName { get; set; }
            public object PropertyValue { get; set; }
        }
    }
}