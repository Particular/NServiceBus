namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Sagas;

class PropertySagaFinder<TSagaData, TMessage>(string sagaPropertyName, Func<TMessage, object> propertyAccessor) : ICoreSagaFinder
    where TSagaData : class, IContainSagaData
{
    public bool IsCustomFinder => false;

    public async Task<IContainSagaData> Find(IServiceProvider builder, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        var propertyValue = propertyAccessor((TMessage)message);

        var lookupValues = context.GetOrCreate<SagaLookupValues>();
        lookupValues.Add<TSagaData>(sagaPropertyName, propertyValue);

        if (propertyValue == null)
        {
            var saga = context.Get<ActiveSagaInstance>();
            var sagaEntityName = saga.Metadata.Name;
            var messageName = typeof(TMessage).FullName;

            throw new Exception($"Message {messageName} mapped to saga {sagaEntityName} has attempted to assign null to the correlation property {sagaPropertyName}. Correlation properties cannot be assigned null.");
        }

        var sagaPersister = builder.GetRequiredService<ISagaPersister>();

        if (sagaPropertyName.Equals("id", StringComparison.OrdinalIgnoreCase))
        {
            return await sagaPersister.Get<TSagaData>((Guid)propertyValue, storageSession, context, cancellationToken).ConfigureAwait(false);
        }

        return await sagaPersister.Get<TSagaData>(sagaPropertyName, propertyValue, storageSession, context, cancellationToken).ConfigureAwait(false);
    }
}