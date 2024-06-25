namespace NServiceBus.Unicast.Messages;

using System;

static class AssemblyQualifiedNameParser
{
    public static string GetMessageTypeNameWithoutAssembly(string messageTypeIdentifier)
    {
        int lastIndexOf = messageTypeIdentifier.LastIndexOf(']');
        if (lastIndexOf > 0)
        {
            var messageType = messageTypeIdentifier.AsSpan()[..++lastIndexOf].ToString();
            return messageType;
        }

        int firstIndexOfComma = messageTypeIdentifier.IndexOf(',');
        if (firstIndexOfComma > 0)
        {
            var messageType = messageTypeIdentifier.AsSpan()[..firstIndexOfComma].ToString();
            return messageType;
        }

        return messageTypeIdentifier;
    }
}