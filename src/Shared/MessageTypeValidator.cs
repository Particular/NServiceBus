namespace NServiceBus;

using System;
using System.Diagnostics.CodeAnalysis;

static class MessageTypeValidator
{
    internal static void Validate(object message, [DynamicallyAccessedMembers(MessageTypeAccess)] Type messageType)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(messageType);
        if (!messageType.IsInstanceOfType(message))
        {
            throw new ArgumentException($"The message instance of type '{message.GetType()}' is not assignable to the declared message type '{messageType}'.", nameof(message));
        }
    }

    const DynamicallyAccessedMemberTypes MessageTypeAccess = DynamicallyAccessedMemberTypes.PublicConstructors
                                                             | DynamicallyAccessedMemberTypes.NonPublicConstructors
                                                             | DynamicallyAccessedMemberTypes.PublicProperties
                                                             | DynamicallyAccessedMemberTypes.Interfaces;
}
