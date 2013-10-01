namespace NServiceBus.Serializers.XML
{
    using System;
    using Utils.Reflection;

    class PropertyMetaData
    {
        public LateBoundProperty LateBoundProperty;
        public LateBoundPropertySet LateBoundPropertySet;
        public Type PropertyType;
        public bool IsIndexed;
    }
}