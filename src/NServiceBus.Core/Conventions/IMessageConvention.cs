namespace NServiceBus
{
    using System;

    /// <summary>
    /// A set of conventions for determining if a class represents a message, command, or event.
    /// </summary>
    public interface IMessageConvention
    {
        /// <summary>
        /// The name of the convention. Used for diagnostic purposes.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determine if a type is a message type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>true if <paramref name="type"/> represents a message.</returns>
        bool IsMessageType(Type type);

        /// <summary>
        /// Determine if a type is a command type.
        /// </summary>
        /// <param name="type">The type to check.</param>.
        /// <returns>true if <paramref name="type"/> represents a command.</returns>
        bool IsCommandType(Type type);

        /// <summary>
        /// Determine if a type is an event type.
        /// </summary>
        /// <param name="type">The type to check.</param>.
        /// <returns>true if <paramref name="type"/> represents an event.</returns>
        bool IsEventType(Type type);
    }
}