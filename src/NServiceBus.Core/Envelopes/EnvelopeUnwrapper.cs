#nullable enable

namespace NServiceBus;

using System;
using Logging;
using Transport;

class EnvelopeUnwrapper(IEnvelopeHandler[] envelopeHandlers, IncomingPipelineMetrics metrics)
{
    static IncomingMessage GetDefaultIncomingMessage(MessageContext messageContext) => new(messageContext.NativeMessageId, messageContext.Headers, messageContext.Body);

    internal IncomingMessageHandle UnwrapEnvelope(MessageContext messageContext)
    {
        var bufferWriter = new LazyArrayPoolBufferWriter<byte>(messageContext.Body.Length);
        foreach (var envelopeHandler in envelopeHandlers)
        {
            try
            {
                if (Log.IsDebugEnabled)
                {
                    Log.DebugFormat(
                        "Unwrapping the current message (NativeID: {0} using {1}", messageContext.NativeMessageId, envelopeHandler.GetType().Name);
                }

                var headers = envelopeHandler.UnwrapEnvelope(messageContext.NativeMessageId, messageContext.Headers,
                    messageContext.Body.Span, messageContext.Extensions, bufferWriter);

                metrics.EnvelopeUnwrappingSucceeded(messageContext, envelopeHandler);

                if (headers is not null)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.DebugFormat(
                            "Unwrapped the message (NativeID: {0} using {1}", messageContext.NativeMessageId, envelopeHandler.GetType().Name);
                    }

                    return new IncomingMessageHandle(new IncomingMessage(messageContext.NativeMessageId, headers, bufferWriter.WrittenMemory), bufferWriter);
                }

                // No-op when nothing written
                bufferWriter.Clear();

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

        return new IncomingMessageHandle(GetDefaultIncomingMessage(messageContext), bufferWriter);
    }

    internal readonly struct IncomingMessageHandle(IncomingMessage message, IDisposable disposable) : IDisposable
    {
        public IncomingMessage Message { get; } = message;

        public void Dispose() => disposable.Dispose();

        public static implicit operator IncomingMessage(IncomingMessageHandle handle) => handle.Message;
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}