namespace NServiceBus;

using System;
using System.Collections.Generic;

class SagaLookupValues
{
    public void Add<TSagaData>(object propertyValue) => entries[typeof(TSagaData)] = propertyValue;

    public bool TryGet(Type sagaType, out object value) => entries.TryGetValue(sagaType, out value);

    readonly Dictionary<Type, object> entries = [];
}