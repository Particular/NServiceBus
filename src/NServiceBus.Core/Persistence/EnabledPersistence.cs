namespace NServiceBus;

using System;
using System.Collections.Generic;

class EnabledPersistence
{
    public List<StorageType> SelectedStorages { get; set; }
    public Type DefinitionType;
}