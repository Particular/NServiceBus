namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Sagas;

class CustomFinderAdapter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFinder, TSagaData, TMessage> : ICoreSagaFinder where TFinder : ISagaFinder<TSagaData, TMessage> where TSagaData : class, IContainSagaData
{
    public bool IsCustomFinder => true;

    public async Task<IContainSagaData> Find(IServiceProvider serviceProvider, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
    {
        var finder = factory(serviceProvider, []);
        await using var _ = Disposable.Wrap(finder).ConfigureAwait(false);

        return await finder
            .FindBy((TMessage)message, storageSession, context, cancellationToken)
            .ThrowIfNull()
            .ConfigureAwait(false);
    }

    static readonly ObjectFactory<TFinder> factory = ActivatorUtilities.CreateFactory<TFinder>([]);
}