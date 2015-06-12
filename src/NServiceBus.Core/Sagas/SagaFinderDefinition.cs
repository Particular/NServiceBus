namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a message finder
    /// </summary>
    public class SagaFinderDefinition   
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SagaFinderDefinition"/> class.
        /// </summary>
        /// <param name="type">The type of the finder.</param>
        /// <param name="messageType">The type of message this finder is associated with.</param>
        /// <param name="properties">Custom properties.</param>
        public SagaFinderDefinition(Type type, string messageType, Dictionary<string, object> properties)
        {
            Type = type;
            MessageType = messageType;
            Properties = properties;
        }

        /// <summary>
        /// The type of the finder.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// The type of message this finder is associated with.
        /// </summary>
        public string MessageType { get; private set; }

        /// <summary>
        /// Custom properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; private set; }
    }
}