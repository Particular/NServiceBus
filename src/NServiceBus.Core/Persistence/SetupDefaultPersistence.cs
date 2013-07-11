namespace NServiceBus.Persistence
{
    public class SetupDefaultPersistence : IWantToRunBeforeConfiguration
    {
        public void Init()
        {
            ConfigureRavenPersistence.RegisterDefaults();
        }
    }
}