namespace NServiceBus.Persistence
{
    using System;

    class EnabledPersistence
    {
        public Type DefinitionType;
        public Action<PersistenceConfiguration> Customizations;
    }
}