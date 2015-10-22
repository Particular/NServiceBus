namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Persistence;
    using NServiceBus.Sagas;

    class CustomFinderAdapter<TSagaData, TMessage> : SagaFinder where TSagaData : IContainSagaData
    {
        public override async Task<IContainSagaData> Find(IBuilder builder, SagaFinderDefinition finderDefinition, SynchronizedStorageSession storageSession, ContextBag context, object message)
        {
            var customFinderType = (Type) finderDefinition.Properties["custom-finder-clr-type"];

            var finder = (IFindSagas<TSagaData>.Using<TMessage>) builder.Build(customFinderType);

            return await finder.FindBy((TMessage) message, storageSession, context).ConfigureAwait(false);
        }
    }
}