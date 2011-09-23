using System;
using System.Runtime.Serialization;

namespace NServiceBus.Hosting
{
    [Serializable]
    public class UnableToKillProcessException : Exception
    {
        public UnableToKillProcessException() { }

        public UnableToKillProcessException(string message) : base(message) { }

        public UnableToKillProcessException(string message, Exception inner) : base(message, inner) { }

        protected UnableToKillProcessException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}