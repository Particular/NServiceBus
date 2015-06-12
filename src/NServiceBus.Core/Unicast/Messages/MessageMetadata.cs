namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Message metadata class.
    /// </summary>
    public class MessageMetadata
    {
        Type messageType;
        bool recoverable;
        IEnumerable<Type> messageHierarchy;
        TimeSpan timeToBeReceived;

        internal MessageMetadata(Type messageType = null, bool recoverable = false, TimeSpan? timeToBeReceived = null, IEnumerable<Type> messageHierarchy = null)
        {
            this.messageType = messageType;
            this.recoverable = recoverable;
            this.messageHierarchy = (messageHierarchy == null ? new List<Type>() : new List<Type>(messageHierarchy)).AsReadOnly();
            this.timeToBeReceived = timeToBeReceived ?? TimeSpan.MaxValue;
        }

        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType { get { return messageType; } }

        /// <summary>
        ///     Gets whether or not the message is supposed to be guaranteed deliverable.
        /// </summary>
        public bool Recoverable { get { return recoverable; } }

        /// <summary>
        ///     Gets the maximum time limit in which the message must be received.
        /// </summary>
        public TimeSpan TimeToBeReceived { get { return timeToBeReceived; } }

        /// <summary>
        /// The message instance hierarchy.
        /// </summary>
        public IEnumerable<Type> MessageHierarchy{ get { return messageHierarchy; } }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            return string.Format("MessageType: {0}, Recoverable: {1}, TimeToBeReceived: {2} , Parent types: {3}", MessageType, Recoverable,
                TimeToBeReceived == TimeSpan.MaxValue ? "Not set" : TimeToBeReceived.ToString(), string.Join(";", MessageHierarchy.Select(pt => pt.FullName)));
        }
    }
}