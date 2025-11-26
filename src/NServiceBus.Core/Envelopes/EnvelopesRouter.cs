namespace NServiceBus;

using System;
using System.Collections.Generic;
using Transport;

class EnvelopesRouter(IEnumerable<IEnvelopeHandler> envelopeHandlers)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage Translate(MessageContext messageContext)
    {
        foreach (var envelopeHandler in envelopeHandlers)
        {
            var result = envelopeHandler.UnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body);
            switch (result)
            {
                case EnvelopeUnwrapResult.Malformed { Exception: not null } malformedEnvelope:
                    throw new Exception("Failure while handling the message envelope", malformedEnvelope.Exception);
                case EnvelopeUnwrapResult.Malformed:
                    throw new Exception("Failure while handling the message envelope");
                case EnvelopeUnwrapResult.Success success:
                    return new IncomingMessage(messageContext.NativeMessageId, success.Headers, success.Body);
                case EnvelopeUnwrapResult.UnsupportedEnvelope:
                    // maybe log and move on
                    continue;
                default:
                    continue;
            }
        }

        return GetDefaultIncomingMessage(messageContext);
    }
}