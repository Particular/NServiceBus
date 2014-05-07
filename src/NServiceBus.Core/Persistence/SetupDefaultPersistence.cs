namespace NServiceBus.Persistence
{
    using System;

    public class SetupDefaultPersistence : IWantToRunBeforeConfiguration
    {
        [Obsolete]
        public void Init()
        {
            ConfigureRavenPersistence.RegisterDefaults();
        }
    }
}