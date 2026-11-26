#nullable enable

namespace NServiceBus;

using System;
using System.Linq;

// The parts of an outgoing span name. With the v11 behavior the name is "{operation} {message type}", a new string
// for each operation. The factory builds it only when it created a span, and the failure log only when the operation
// failed without a span, so an operation without a listener allocates nothing for it.
readonly struct OutgoingSpanName
{
    OutgoingSpanName(string legacyName, string? operation, Type? messageType, Type[]? messageTypes)
    {
        this.legacyName = legacyName;
        this.operation = operation;
        this.messageType = messageType;
        this.messageTypes = messageTypes;
    }

    public static OutgoingSpanName For(string legacyName, string operation, Type messageType) =>
        new(legacyName, operation, messageType, null);

    public static OutgoingSpanName For(string legacyName, string operation, Type[] messageTypes) =>
        new(legacyName, operation, null, messageTypes);

    public static implicit operator OutgoingSpanName(string name) => new(name, null, null, null);

    // Remove the switch check and legacyName in v11, see obsoletes-v10.cs.
    public string Build()
    {
        if (!V11BehaviorSwitch.UseV11Behavior || operation is null)
        {
            return legacyName;
        }

        return messageType is not null
            ? $"{operation} {messageType.Name}"
            : $"{operation} {string.Join(' ', messageTypes!.Select(x => x.Name))}";
    }

    readonly string legacyName;
    readonly string? operation;
    readonly Type? messageType;
    readonly Type[]? messageTypes;
}
