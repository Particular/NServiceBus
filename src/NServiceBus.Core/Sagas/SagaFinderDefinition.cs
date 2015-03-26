namespace NServiceBus.Saga
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines a message finder
    /// </summary>
    public class SagaFinderDefinition   
    {
        internal SagaFinderDefinition()
        {
            Properties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Custom properties
        /// </summary>
        public Dictionary<string, object> Properties;
        
        /// <summary>
        /// The type of the finder
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// The type of message this finder is associated with
        /// </summary>
        public string MessageType { get; set; }
    }
}