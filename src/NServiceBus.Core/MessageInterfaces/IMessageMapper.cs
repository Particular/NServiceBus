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
        /// If the given type is an interface, returns the generated concrete type.
        /// If the given type is concrete, returns the interface it was generated from.
        /// </summary>
        Type GetMappedTypeFor(Type t);

        /// <summary>
        /// Looks up the type mapped for the given name.
        /// </summary>
        Type GetMappedTypeFor(string typeName);
    }
}