namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class PropertySagaFinder<TSagaData> : SagaFinder where TSagaData : class, IContainSagaData
    {
        public PropertySagaFinder(ISagaPersister sagaPersister)
        {
            this.sagaPersister = sagaPersister;
        }

        public override async Task<IContainSagaData> Find(IServiceProvider builder, SagaFinderDefinition finderDefinition, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
        {
            var propertyAccessor = (Func<object, object>)finderDefinition.Properties["property-accessor"];
            var propertyValue = propertyAccessor(message);

            var sagaPropertyName = (string)finderDefinition.Properties["saga-property-name"];

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
                return await sagaPersister.Get<TSagaData>((Guid)propertyValue, storageSession, context, cancellationToken).ConfigureAwait(false);
            }

            return await sagaPersister.Get<TSagaData>(sagaPropertyName, propertyValue, storageSession, context, cancellationToken).ConfigureAwait(false);
        }

        ISagaPersister sagaPersister;
    }
}