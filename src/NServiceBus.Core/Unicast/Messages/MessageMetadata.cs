﻿namespace NServiceBus.Unicast.Messages
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Message metadata class.
    /// </summary>
    public partial class MessageMetadata
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public MessageMetadata(Type messageType, IEnumerable<Type> messageHierarchy = null)
        {
            MessageType = messageType;
            MessageHierarchy = (messageHierarchy == null ? new List<Type>() : new List<Type>(messageHierarchy)).AsReadOnly();
        }

        /// <summary>
        /// The <see cref="Type"/> of the message instance.
        /// </summary>
        public Type MessageType { get; private set; }

     
        /// <summary>
        /// The message instance hierarchy.
        /// </summary>
        public IEnumerable<Type> MessageHierarchy { get; private set; }
    }
}