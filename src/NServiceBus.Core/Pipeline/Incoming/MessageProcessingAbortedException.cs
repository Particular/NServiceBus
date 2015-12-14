namespace NServiceBus
{
    using System;
    using System.Runtime.Serialization;
    using JetBrains.Annotations;
    using NServiceBus.Features;
    using NServiceBus.Transports;

    /// <summary>
    /// This exception can be thrown by any of recoverability behaviors ie.e <see cref="FirstLevelRetries"/>, <see cref="SecondLevelRetries"/> or <see cref="StoreFaultsInErrorQueue"/>
    /// It is meant to indicate to the <see cref="IPushMessages"/> that message should be returned to input queue and will be handeled by recoverability mechanisms on next receive.
    /// </summary>
    [Serializable]
    public class MessageProcessingAbortedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        public MessageProcessingAbortedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="MessageProcessingAbortedException"/>.
        /// </summary>
        protected MessageProcessingAbortedException([NotNull] SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}