namespace NServiceBus.Persistence
{
    using System;
    using System.Collections.Generic;

    class EnabledPersistence
    {
        public Type DefinitionType;
        public List<Type> SelectedStorages { get; set; }
    }
}