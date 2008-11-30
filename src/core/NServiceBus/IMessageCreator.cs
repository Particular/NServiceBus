using System;

namespace NServiceBus
{
    public interface IMessageCreator
    {
        T CreateInstance<T>() where T : IMessage;
        T CreateInstance<T>(Action<T> action) where T : IMessage;
        object CreateInstance(Type messageType);
    }
}
