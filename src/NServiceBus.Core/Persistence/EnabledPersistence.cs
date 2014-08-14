namespace NServiceBus.Persistence
{
    using System;

    class EnabledPersistence
    {
        public Type DefinitionType;
        public Storage[] SelectedStorages { get; set; }
    }
}