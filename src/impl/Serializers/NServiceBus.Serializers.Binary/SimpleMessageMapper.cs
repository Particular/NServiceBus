using System;
using NServiceBus.MessageInterfaces;

namespace NServiceBus.Serializers.Binary
{
    /// <summary>
    /// Simple implementation of message mapper for binary serialization.
    /// </summary>
    public class SimpleMessageMapper : IMessageMapper
    {
        T IMessageCreator.CreateInstance<T>()
        {
            return Activator.CreateInstance<T>();
        }

        T IMessageCreator.CreateInstance<T>(Action<T> action)
        {
            T result = Activator.CreateInstance<T>();
            action(result);

            return result;
        }

        object IMessageCreator.CreateInstance(Type messageType)
        {
            return Activator.CreateInstance(messageType);
        }

        void IMessageMapper.Initialize(params Type[] types)
        {
        }

        Type IMessageMapper.GetMappedTypeFor(Type t)
        {
            return t;
        }

        Type IMessageMapper.GetMappedTypeFor(string typeName)
        {
            return Type.GetType(typeName);
        }
    }
}
