using System;

namespace NServiceBus.Unicast.Transport
{
    [Serializable]
    public class ErrorMessage : IMessage
    {
        public int ErrorCode;
    }
}
