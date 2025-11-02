namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Sagas;

class HeaderPropertySagaFinder<TSagaData>(string headerName, string correlationPropertyName, Type correlationPropertyType) : ICoreSagaFinder
    where TSagaData : class, IContainSagaData
{
    public async Task<IContainSagaData> Find(IServiceProvider builder, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        if (!messageHeaders.TryGetValue(headerName, out var messageHeaderValue))
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = message.GetType().FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} is missing a header used for correlation: {headerName}.");
        }

        object convertedHeaderValue;

        try
        {
            convertedHeaderValue = TypeDescriptor.GetConverter(correlationPropertyType).ConvertFromInvariantString(messageHeaderValue);
        }
        catch (Exception exception)
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = message.GetType().FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} contains correlation header {headerName} value that cannot be cast to correlation property type {correlationPropertyType}: {messageHeaderValue}", exception);
        }

        var lookupValues = context.GetOrCreate<SagaLookupValues>();
        lookupValues.Add<TSagaData>(correlationPropertyName, convertedHeaderValue);

        if (convertedHeaderValue == null)
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = message.GetType().FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} has attempted to assign null to the correlation property {correlationPropertyName}. Correlation properties cannot be assigned null.");
        }

        var persister = builder.GetRequiredService<ISagaPersister>();
        if (correlationPropertyName.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return await persister.Get<TSagaData>((Guid)convertedHeaderValue, storageSession, context, cancellationToken).ConfigureAwait(false);
        }

        return await persister.Get<TSagaData>(correlationPropertyName, convertedHeaderValue, storageSession, context, cancellationToken).ConfigureAwait(false);
    }
}