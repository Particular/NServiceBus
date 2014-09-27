namespace NServiceBus.Sagas
{
    using System;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Messages;

    class CustomFinderAdapter<TSagaData,TMessage> : SagaFinder where TSagaData : IContainSagaData
    {
        internal override IContainSagaData Find(IBuilder builder,SagaFinderDefinition finderDefinition, LogicalMessage message)
        {
            var customFinderType = (Type)finderDefinition.Properties["custom-finder-clr-type"];

            var finder = (IFindSagas<TSagaData>.Using<TMessage>)builder.Build(customFinderType);

            return finder.FindBy((TMessage) message.Instance);
        }
    }
}