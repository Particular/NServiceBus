namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections.Generic;

    class TypeMetaData
    {
        public Type ListType;
        public bool IsSimpleType;
        public bool IsXContainer;
        public bool IsGenericEnumerable;
        public bool IsSet;
        public bool IsArray;
        public Dictionary<string, PropertyMetaData> Properties = new Dictionary<string, PropertyMetaData>();
        public Dictionary<string, FieldMetaData> Fields = new Dictionary<string, FieldMetaData>();
        public bool IsList;
    }
}