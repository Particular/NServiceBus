namespace NServiceBus
{
    using Extensibility;
    using ObjectBuilder;
    using Persistence;
    using Sagas;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;

    class HeaderPropertySagaFinder<TSagaData> : SagaFinder where TSagaData : class, IContainSagaData
    {
        ISagaPersister persister;

        public HeaderPropertySagaFinder(ISagaPersister persister)
        {
            this.persister = persister;
        }

        public override async Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders)
        {
            var headerName = (string)finderDefinition.Properties["message-header-name"];

            if (!messageHeaders.TryGetValue(headerName, out var messageHeaderValue))
            {
                var saga = context.Get<ActiveSagaInstance>();
                var sagaEntityName = saga.Metadata.Name;
                var messageName = finderDefinition.MessageTypeName;

                throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} is missing a header used for correlation: {headerName}.");
            }

            var correlationPropertyName = (string)finderDefinition.Properties["saga-property-name"];
            var correlationPropertyType = (Type)finderDefinition.Properties["saga-property-type"];

            object convertedHeaderValue;

            try
            {
                convertedHeaderValue = TypeDescriptor.GetConverter(correlationPropertyType).ConvertFromInvariantString(messageHeaderValue);
            }
            catch (Exception exception)
            {
                var saga = context.Get<ActiveSagaInstance>();
                var sagaEntityName = saga.Metadata.Name;
                var messageName = finderDefinition.MessageTypeName;

                throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} contains correlation header {headerName} value that cannot be cast to correlation property type {correlationPropertyType}: {messageHeaderValue}", exception);
            }

            var lookupValues = context.GetOrCreate<SagaLookupValues>();
            lookupValues.Add<TSagaData>(correlationPropertyName, convertedHeaderValue);

            if (convertedHeaderValue == null)
            {
                var saga = context.Get<ActiveSagaInstance>();
                var sagaEntityName = saga.Metadata.Name;
                var messageName = finderDefinition.MessageTypeName;

                throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} has attempted to assign null to the correlation property {correlationPropertyName}. Correlation properties cannot be assigned null.");
            }

            if (correlationPropertyName.ToLower() == "id")
            {
                return await persister.Get<TSagaData>((Guid)convertedHeaderValue, storageSession, context).ConfigureAwait(false);
            }

            return await persister.Get<TSagaData>(correlationPropertyName, convertedHeaderValue, storageSession, context).ConfigureAwait(false);
        }
    }
}