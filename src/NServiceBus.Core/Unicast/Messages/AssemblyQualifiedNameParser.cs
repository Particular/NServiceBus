namespace NServiceBus.Unicast.Messages;

static class AssemblyQualifiedNameParser
{
    public static string GetMessageTypeNameWithoutAssemblyOld(string messageTypeIdentifier)
    {
        var typeParts = messageTypeIdentifier.Split(',');
        if (typeParts.Length > 1)
        {
            return typeParts[0]; //Take the type part only
        }

        return messageTypeIdentifier;
    }
}