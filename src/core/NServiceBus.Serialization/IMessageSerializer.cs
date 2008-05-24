using System;
using System.IO;

namespace NServiceBus.Serialization
{
    public interface IMessageSerializer
    {
        void Initialize(params Type[] types);
        void Serialize(IMessage[] messages, Stream stream);
        IMessage[] Deserialize(Stream stream);
    }
}
