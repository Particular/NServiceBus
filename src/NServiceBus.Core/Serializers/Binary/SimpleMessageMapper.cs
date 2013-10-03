namespace NServiceBus.Serializers.Binary
{
    using System;
    using System.Collections.Generic;
    using MessageInterfaces;

    /// <summary>
    /// Simple implementation of message mapper for binary serialization.
    /// </summary>
    public class SimpleMessageMapper : IMessageMapper
    {
        public T CreateInstance<T>()
        {
            return CreateInstance<T>(null);
        }

        public T CreateInstance<T>(Action<T> action)
        {
            var result = (T)CreateInstance(typeof(T));
            if (action != null)
            {
                action(result);
            }

            return result;
        }

        public object CreateInstance(Type messageType)
        {
            if (messageType.IsInterface || messageType.IsAbstract)
                throw new NotSupportedException("The binary serializer does not support interface types. Please use the XML serializer if you need this functionality.");

            return Activator.CreateInstance(messageType);
        }

        public void Initialize(IEnumerable<Type> types)
        {
        }

        public Type GetMessageType(Type concreteType)
        {
            return concreteType;
        }

        public Type GetConcreteType(Type messageType)
        {
            return messageType;
        }

        Type GetMappedTypeFor(Type t)
        {
            return t;
        }

        public Type GetMappedTypeFor(string typeName)
        {
            return Type.GetType(typeName);
        }
    }
}
