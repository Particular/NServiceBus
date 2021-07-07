namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    abstract class SagaFinder
    {
        public abstract Task<IContainSagaData> Find(IServiceProvider builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message, HeaderDictionary messageHeaders, CancellationToken cancellationToken = default);
    }
}