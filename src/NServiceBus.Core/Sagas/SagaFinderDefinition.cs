namespace NServiceBus.Sagas
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a message finder.
    /// </summary>
    public class SagaFinderDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SagaFinderDefinition" /> class.
        /// </summary>
        /// <param name="type">The type of the finder.</param>
        /// <param name="messageType">The type of message this finder is associated with.</param>
        /// <param name="properties">Custom properties.</param>
        internal SagaFinderDefinition(Type type, Type messageType, Dictionary<string, object> properties)
        {
            Type = type;
            MessageType = messageType;
            MessageTypeName = messageType.FullName;
            Properties = properties;
        }

        /// <summary>
        /// The type of the finder.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The type of message this finder is associated with.
        /// </summary>
        public Type MessageType { get; private set; }

        /// <summary>
        /// The full name of the message type.
        /// </summary>
        public string MessageTypeName { get; private set; }

        /// <summary>
        /// Custom properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; private set; }
    }
}