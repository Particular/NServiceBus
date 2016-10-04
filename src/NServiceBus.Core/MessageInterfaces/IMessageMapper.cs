namespace NServiceBus.MessageInterfaces
{
    using System;

    /// <summary>
    /// Enables looking up interfaced mapped to generated concrete types.
    /// and vice versa.
    /// </summary>
    public interface IMessageMapper
    {
        /// <summary>
        /// If the given type is an interface, returns the generated concrete type.
        /// If the given type is concrete, returns the interface it was generated from.
        /// </summary>
        Type GetMappedTypeFor(Type t);
    }
}