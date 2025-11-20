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
    public async Task<IContainSagaData> Find(IServiceProvider serviceProvider, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        var finder = factory(serviceProvider, []);

        try
        {
            return await finder
                .FindBy((TMessage)message, storageSession, context, cancellationToken)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }
        finally
        {
            if (finder is IAsyncDisposable asyncDisposableInstaller)
            {
                await asyncDisposableInstaller.DisposeAsync().ConfigureAwait(false);
            }
            else if (finder is IDisposable disposableInstaller)
            {
                disposableInstaller.Dispose();
            }
        }
    }

    static readonly ObjectFactory<TFinder> factory = ActivatorUtilities.CreateFactory<TFinder>([]);
}