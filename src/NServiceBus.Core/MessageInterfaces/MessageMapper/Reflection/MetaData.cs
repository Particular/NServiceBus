namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    using System;

    class MetaData
    {
        public Type ConcreteType;
        public Type MessageType;
        public string TypeFullName;
        public Func<object> ConstructInstance;
    }
}