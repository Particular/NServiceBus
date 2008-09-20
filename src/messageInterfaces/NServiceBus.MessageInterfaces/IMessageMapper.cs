using System;

namespace NServiceBus.MessageInterfaces
{
    public interface IMessageMapper : IMessageCreator
    {
        void Initialize(params Type[] types);
        Type GetMappedTypeFor(Type t);
        Type GetMappedTypeFor(string typeName);
    }
}
