namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Microsoft.Extensions.DependencyInjection;
    using Persistence;
    using Sagas;

    class CustomFinderAdapter<TSagaData, TMessage> : SagaFinder where TSagaData : IContainSagaData
    {
        public override async Task<IContainSagaData> Find(IServiceProvider builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message, IDictionary<string, string> messageHeaders, CancellationToken cancellationToken = default)
        {
            var customFinderType = (Type)finderDefinition.Properties["custom-finder-clr-type"];

            var finder = (IFindSagas<TSagaData>.Using<TMessage>)builder.GetRequiredService(customFinderType);

            return await finder
                .FindBy((TMessage)message, storageSession, context, cancellationToken)
                .ThrowIfNull()
                .ConfigureAwait(false);
        }
    }
}