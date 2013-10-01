namespace NServiceBus.Serializers.XML
{
    using System;
    using Utils.Reflection;

    class FieldMetaData
    {
        public LateBoundFieldSet LateBoundFieldSet { get; set; }
        public LateBoundField LateBoundField { get; set; }
        public Type FieldType;
    }
}