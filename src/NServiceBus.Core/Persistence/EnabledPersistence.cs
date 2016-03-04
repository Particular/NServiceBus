namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class EnabledPersistence
    {
        public List<Type> SelectedStorages { get; set; }
        public Type DefinitionType;
    }
}