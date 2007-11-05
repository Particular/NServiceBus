using System;
using System.Collections.Generic;
using System.Text;

namespace NServiceBus
{
    public interface IMessageHandler<T> where T : IMessage
    {
        void Handle(T message);
    }
}
