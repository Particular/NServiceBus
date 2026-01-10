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
                    Log.Debug(
                        $"Unwrapping the current message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                }

                (Dictionary<string, string> headers, ReadOnlyMemory<byte> body)? unwrappingResult = envelopeHandler.UnwrapEnvelope(
                    messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions,
                    messageContext.Body);

                metrics.EnvelopeUnwrappingSucceeded(messageContext, envelopeHandler);

                if (unwrappingResult.HasValue)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Unwrapped the message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                    }

                    return new IncomingMessage(messageContext.NativeMessageId, unwrappingResult.Value.headers, unwrappingResult.Value.body);
                }

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Did not unwrap the message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                }
            }
            catch (Exception e)
            {
                metrics.EnvelopeUnwrappingFailed(messageContext, envelopeHandler, e);
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
            if (envelopeHandlers.Length > 0)
            {
                Log.Debug(
                    $"No envelope handler unwrapped the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format");
            }
            else
            {
                Log.Debug(
                    $"No envelope handler found for the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format");
            }
        }

        return GetDefaultIncomingMessage(messageContext);
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}