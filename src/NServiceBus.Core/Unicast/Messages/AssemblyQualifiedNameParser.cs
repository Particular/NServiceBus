namespace NServiceBus;

using System;

static class AssemblyQualifiedNameParser
{
    public static ReadOnlySpan<char> GetMessageTypeNameWithoutAssembly(ReadOnlySpan<char> messageTypeIdentifier)
    {
        int lastIndexOf = messageTypeIdentifier.LastIndexOf(']');
        if (lastIndexOf > 0)
        {
            return messageTypeIdentifier[..++lastIndexOf];
        }

        int firstIndexOfComma = messageTypeIdentifier.IndexOf(',');
        if (firstIndexOfComma > 0)
        {
            return messageTypeIdentifier[..firstIndexOfComma];
        }

        return messageTypeIdentifier;
    }
}