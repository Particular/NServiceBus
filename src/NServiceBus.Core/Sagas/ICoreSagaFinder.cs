namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Extensibility;
using Persistence;

interface ICoreSagaFinder
{
    bool IsCustomFinder { get; }

    Task<IContainSagaData> Find(IServiceProvider serviceProvider, ISynchronizedStorageSession storageSession, ContextBag context, object message, IReadOnlyDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default);
}