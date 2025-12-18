#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using Logging;
using Transport;

class EnvelopeUnwrapper(IEnvelopeHandler[] envelopeHandlers, IncomingPipelineMetrics metrics)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessage UnwrapEnvelope(MessageContext messageContext)
    {
        // TODO: Is there any point in optimizing this to never hit the foreach if the translators list is empty.
        // https://stackoverflow.com/questions/45651325/performance-before-using-a-foreach-loop-check-if-the-list-is-empty
        foreach (var envelopeHandler in envelopeHandlers)
        {
            try
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Unwrapping the current message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                }

                (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? unwrappingResult = envelopeHandler.UnwrapEnvelope(
                    messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions,
                    messageContext.Body);

                if (unwrappingResult.HasValue)
                {
                    return new IncomingMessage(messageContext.NativeMessageId, unwrappingResult.Value.headers, unwrappingResult.Value.body);
                }
            }
            catch (Exception e)
            {
                metrics.RecordEnvelopeUnwrappingError(messageContext, envelopeHandler);
                if (Log.IsWarnEnabled)
                {
                    Log.Warn(
                        $"Unwrapper {envelopeHandler} failed to unwrap the message {messageContext.NativeMessageId}",
                        e);
                }
            }
        }

        if (Log.IsDebugEnabled)
        {
            Log.Debug(
                $"No envelope handler found for the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format");
        }

        return GetDefaultIncomingMessage(messageContext);
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}