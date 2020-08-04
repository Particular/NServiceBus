namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class SagaLookupValues
    {
        public void Add<TSagaData>(string propertyName, object propertyValue)
        {
            entries[typeof(TSagaData)] = new LookupValue
            {
                PropertyName = propertyName,
                PropertyValue = propertyValue
            };
        }

        public bool TryGet(Type sagaType, out LookupValue value)
        {
            return entries.TryGetValue(sagaType, out value);
        }

        Dictionary<Type, LookupValue> entries = new Dictionary<Type, LookupValue>();

        public class LookupValue
        {
            public string PropertyName { get; set; }
            public object PropertyValue { get; set; }
        }
    }
}