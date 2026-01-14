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
        foreach (var envelopeHandler in envelopeHandlers)
        {
            try
            {
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat(
                        "Unwrapping the current message (NativeID: {0} using {1}", messageContext.NativeMessageId, envelopeHandler.GetType().Name);
                }

                (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? unwrappingResult = envelopeHandler.UnwrapEnvelope(
                    messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions,
                    messageContext.Body);

                metrics.EnvelopeUnwrappingSucceeded(messageContext, envelopeHandler);

                if (unwrappingResult.HasValue)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat(
                            "Unwrapped the message (NativeID: {0} using {1}", messageContext.NativeMessageId, envelopeHandler.GetType().Name);
                    }

                    return new IncomingMessage(messageContext.NativeMessageId, unwrappingResult.Value.headers, unwrappingResult.Value.body);
                }

                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat(
                        "Did not unwrap the message (NativeID: {0} using {1}", messageContext.NativeMessageId, envelopeHandler.GetType().Name);
                }
            }
            catch (Exception e)
            {
                metrics.EnvelopeUnwrappingFailed(messageContext, envelopeHandler, e);
                if (Log.IsWarnEnabled)
                {
                    Log.WarnFormat(
                        "Unwrapper {0} failed to unwrap the message {1}: {2}", envelopeHandler, messageContext.NativeMessageId, e);
                }
            }
        }

        if (Log.IsDebugEnabled)
        {
            if (envelopeHandlers.Length > 0)
            {
                Log.DebugFormat(
                    "No envelope handler unwrapped the current message (NativeID: {0}, assuming the default NServiceBus format", messageContext.NativeMessageId);
            }
            else
            {
                Log.DebugFormat(
                    "No envelope handler found for the current message (NativeID: {0}, assuming the default NServiceBus format", messageContext.NativeMessageId);
            }
        }

        return GetDefaultIncomingMessage(messageContext);
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}