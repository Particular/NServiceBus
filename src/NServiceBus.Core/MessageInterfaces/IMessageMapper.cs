namespace NServiceBus.MessageInterfaces
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Enables looking up interfaced mapped to generated concrete types.
    /// and vice versa.
    /// </summary>
    public interface IMessageMapper : IMessageCreator
    {
        /// <summary>
        /// Initializes the mapper with the given types to be scanned.
        /// </summary>
        void Initialize(IEnumerable<Type> types);

        /// <summary>
        /// Returns the message implementation for a given concrete type
        /// </summary>
        Type GetMessageType(Type concreteType);

        /// <summary>
        /// Get the concrete type for a given message type.
        /// </summary>
        Type GetConcreteType(Type messageType);

        /// <summary>
        /// Looks up the type mapped for the given name.
        /// </summary>
        Type GetMappedTypeFor(string typeName);
    }
}
