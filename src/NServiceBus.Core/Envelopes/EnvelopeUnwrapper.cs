#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Logging;
using Transport;

class EnvelopeUnwrapper(IEnvelopeHandler[] envelopeHandlers)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage UnwrapEnvelope(MessageContext messageContext)
    {
        // TODO: Is there any point in optimizing this to never hit the foreach if the translators list is empty.
        // https://stackoverflow.com/questions/45651325/performance-before-using-a-foreach-loop-check-if-the-list-is-empty
        foreach (var envelopeHandler in envelopeHandlers)
        {
            if (envelopeHandler.CanUnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body))
            {
                Log.Debug($"Unwrapping the current message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                (Dictionary<string, string> headers, ReadOnlyMemory<byte> body) = envelopeHandler.UnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions, messageContext.Body);
                return new IncomingMessage(messageContext.NativeMessageId, headers, body);
            }
        }

        Log.Debug($"No envelope handler found for the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format");
        return GetDefaultIncomingMessage(messageContext);
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}