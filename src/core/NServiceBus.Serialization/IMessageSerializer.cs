using System;
using System.Collections.Generic;
using System.IO;

namespace NServiceBus.Serialization
{
    public interface IMessageSerializer
    {
        void Initialize(params Type[] types);
        void Serialize(IEnumerable<IMessage> messages, Stream stream);
        IEnumerable<IMessage> Deserialize(Stream stream);
    }
}
