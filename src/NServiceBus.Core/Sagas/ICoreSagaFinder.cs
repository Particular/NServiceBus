namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;
using Sagas;

interface ICoreSagaFinder
{
    Task<IContainSagaData> Find(IServiceProvider builder, SagaFinderDefinition finderDefinition, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default);
}