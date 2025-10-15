namespace NServiceBus;

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Transport;

class UnmarshalingComponent
{
    public static UnmarshalingComponent Initialize(HostingComponent.Configuration hostingConfiguration)
    {
        hostingConfiguration.Services.AddSingleton<UnmarshalingRouter>();
        return new UnmarshalingComponent();
    }
}

class UnmarshalingRouter(IEnumerable<IUnmarshalMessages> translators)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage Translate(MessageContext messageContext)
    {
        foreach (var translator in translators)
        {
            if (translator.IsValidMessage(messageContext))
            {
                return translator.CreateIncomingMessage(messageContext);
            }
        }
        return GetDefaultIncomingMessage(messageContext);
    }
}