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
        internal MessageMetadata(Type messageType = null, bool recoverable = false, TimeSpan? timeToBeReceived = null, IEnumerable<Type> messageHierarchy = null)
        {
            MessageType = messageType;
            Recoverable = recoverable;
            MessageHierarchy = (messageHierarchy == null ? new List<Type>() : new List<Type>(messageHierarchy)).AsReadOnly();
            TimeToBeReceived = timeToBeReceived ?? TimeSpan.MaxValue;
        }

        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }

        /// <summary>
        ///     Gets whether or not the message is supposed to be guaranteed deliverable.
        /// </summary>
        public bool Recoverable { get; private set; }

        /// <summary>
        ///     Gets the maximum time limit in which the message must be received.
        /// </summary>
        public TimeSpan TimeToBeReceived { get; private set; }

        /// <summary>
        /// The message instance hierarchy.
        /// </summary>
        public IEnumerable<Type> MessageHierarchy { get; private set; }

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