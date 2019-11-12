namespace NServiceBus.Features
{
    using NServiceBus.Outbox;
    using Persistence;
    using Unicast;

    class ReceiveFeature : Feature
    {
        public ReceiveFeature()
        {
            EnableByDefault();
        }

        protected internal override void Setup(FeatureConfigurationContext context)
        {
            context.Pipeline.Register("TransportReceiveToPhysicalMessageProcessingConnector", b =>
            {
                var storage = context.Container.HasComponent<IOutboxStorage>() ? b.Build<IOutboxStorage>() : new NoOpOutboxStorage();
                return new TransportReceiveToPhysicalMessageConnector(storage);
            }, "Allows to abort processing the message");

            context.Pipeline.Register("LoadHandlersConnector", b =>
            {
                var adapter = context.Container.HasComponent<ISynchronizedStorageAdapter>() ? b.Build<ISynchronizedStorageAdapter>() : new NoOpSynchronizedStorageAdapter();
                var syncStorage = context.Container.HasComponent<ISynchronizedStorage>() ? b.Build<ISynchronizedStorage>() : new NoOpSynchronizedStorage();

                return new LoadHandlersConnector(b.Build<MessageHandlerRegistry>(), syncStorage, adapter);
            }, "Gets all the handlers to invoke from the MessageHandler registry based on the message type.");

            context.Pipeline.Register("ExecuteUnitOfWork", new UnitOfWorkBehavior(), "Executes the UoW");

            context.Pipeline.Register("InvokeHandlers", new InvokeHandlerTerminator(), "Calls the IHandleMessages<T>.Handle(T)");
        }
    }
}