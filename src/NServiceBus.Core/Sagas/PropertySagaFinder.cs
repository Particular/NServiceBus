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

        public override async Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message)
        {
            var propertyAccessor = (Func<object, object>) finderDefinition.Properties["property-accessor"];
            var propertyValue = propertyAccessor(message);

            var sagaPropertyName = (string) finderDefinition.Properties["saga-property-name"];

            var lookupValues = context.GetOrCreate<SagaLookupValues>();
            lookupValues.Add<TSagaData>(sagaPropertyName, propertyValue);

            if (propertyValue == null)
            {
                var saga = context.Get<ActiveSagaInstance>();
                var sagaEntityName = saga.Metadata.Name;
                var messageName = finderDefinition.MessageTypeName;

                throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} has attempted to assign null to the correlation property {sagaPropertyName}. Correlation properties cannot be assigned null.");
            }

            if (sagaPropertyName.ToLower() == "id")
            {
                return await sagaPersister.Get<TSagaData>((Guid) propertyValue, storageSession, context).ConfigureAwait(false);
            }

            return await sagaPersister.Get<TSagaData>(sagaPropertyName, propertyValue, storageSession, context).ConfigureAwait(false);
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