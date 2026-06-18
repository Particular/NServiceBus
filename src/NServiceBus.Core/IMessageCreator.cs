#nullable enable

namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// The abstraction for creating interface-based messages.
/// </summary>
public interface IMessageCreator
{
    internal const DynamicallyAccessedMemberTypes CreatorMembersRequired = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors;

    /// <summary>
    /// Creates an instance of the message type T.
    /// </summary>
    /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
    /// <returns>A message object that implements the interface T.</returns>
    T CreateInstance<[DynamicallyAccessedMembers(CreatorMembersRequired)] T>();

    /// <summary>
    /// Creates an instance of the message type T and fills it with data.
    /// </summary>
    /// <typeparam name="T">The type of message interface to instantiate.</typeparam>
    /// <param name="action">An action to set various properties of the instantiated object.</param>
    /// <returns>A message object that implements the interface T.</returns>
    T CreateInstance<[DynamicallyAccessedMembers(CreatorMembersRequired)] T>(Action<T> action);

    /// <summary>
    /// Creates an instance of the given message type.
    /// </summary>
    /// <param name="messageType">The type of message to instantiate.</param>
    /// <returns>A message object that implements the given interface.</returns>
    object CreateInstance([DynamicallyAccessedMembers(CreatorMembersRequired)] Type messageType);
}