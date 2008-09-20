using System;

namespace NServiceBus.MessageInterfaces
{
    public interface IMessageCreator
    {
        T CreateInstance<T>() where T : IMessage;
        object CreateInstance(Type messageType);
    }
}
