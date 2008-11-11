using System;

namespace NServiceBus
{
    public interface IMessageCreator
    {
        T CreateInstance<T>() where T : IMessage;
        object CreateInstance(Type messageType);
    }
}
