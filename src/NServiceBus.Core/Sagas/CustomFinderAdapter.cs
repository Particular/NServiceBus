namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Sagas;

class CustomFinderAdapter<TFinder, TSagaData, TMessage> : ICoreSagaFinder where TFinder : ISagaFinder<TSagaData, TMessage> where TSagaData : IContainSagaData
{
    public async Task<IContainSagaData> Find(IServiceProvider builder, SagaFinderDefinition finderDefinition, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        var finder = factory.Invoke(builder, []);

        return await finder
            .FindBy((TMessage)message, storageSession, context, cancellationToken)
            .ThrowIfNull()
            .ConfigureAwait(false);
    }

    readonly ObjectFactory<TFinder> factory = ActivatorUtilities.CreateFactory<TFinder>([]);
}