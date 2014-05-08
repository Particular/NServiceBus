namespace NServiceBus.Persistence
{
    using InMemory;

    public class SetupDefaultPersistence : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            InMemoryPersistence.UseAsDefault();
        }
    }
}