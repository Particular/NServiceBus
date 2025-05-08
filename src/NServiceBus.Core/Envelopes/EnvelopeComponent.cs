namespace NServiceBus;

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Transport;

class EnvelopeComponent
{
    public static EnvelopeComponent Initialize(HostingComponent.Configuration hostingConfiguration)
    {
        hostingConfiguration.Services.AddSingleton<EnvelopeTranslatorRouter>();
        return new EnvelopeComponent();
    }
}

class EnvelopeTranslatorRouter(IEnumerable<IEnvelopeTranslator> translators)
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


public interface IEnvelopeTranslator
{
    public IncomingMessage CreateIncomingMessage(MessageContext messageContext);
    public bool IsValidMessage(MessageContext messageContext);
}