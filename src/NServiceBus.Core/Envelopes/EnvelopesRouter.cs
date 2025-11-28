namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

class EnvelopesRouter(IEnumerable<IEnvelopeHandler> envelopeHandlers)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage Translate(MessageContext messageContext)
    {
        // TODO: Is there any point in optimizing this to never hit the foreach if the translators list is empty.
        // https://stackoverflow.com/questions/45651325/performance-before-using-a-foreach-loop-check-if-the-list-is-empty
        foreach (var envelopeHandler in envelopeHandlers)
        {
            if (envelopeHandler.CanUnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body))
            {
                // TODO log which envelope handler was used
                (Dictionary<string, string> headers, ReadOnlyMemory<byte> body) = envelopeHandler.UnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body);
                return new IncomingMessage(messageContext.NativeMessageId, headers, body);
            }
        }

        // TODO log the default handler was used
        return GetDefaultIncomingMessage(messageContext);
    }
}