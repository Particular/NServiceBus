namespace NServiceBus.Outbox
{
    using Config;
    using Persistence.InMemory.Outbox;

    public class SetupDefaultPersistence : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            InfrastructureServices.SetDefaultFor<IOutboxStorage>(() => Configure.Component<InMemoryOutboxStorage>(DependencyLifecycle.SingleInstance));
        }
    }
}