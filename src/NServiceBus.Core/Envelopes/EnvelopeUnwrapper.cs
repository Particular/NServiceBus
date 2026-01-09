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
        ArrayPoolBufferWriter<byte>? bufferWriter = null;

        foreach (var envelopeHandler in envelopeHandlers)
        {
            try
            {
                bufferWriter ??= new ArrayPoolBufferWriter<byte>();

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Unwrapping the current message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                }

                var headers = envelopeHandler.UnwrapEnvelope(bufferWriter,
                    messageContext.NativeMessageId, messageContext.Headers, messageContext.Extensions,
                    messageContext.Body.Span);

                metrics.RecordEnvelopeUnwrappingError(messageContext, envelopeHandler, null, true);

                if (headers is not null && bufferWriter.WrittenCount > 0)
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug(
                            $"Unwrapped the message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                    }

                    return new IncomingMessageHandle(new IncomingMessage(messageContext.NativeMessageId, headers, bufferWriter.WrittenMemory), bufferWriter);
                }

                // No-op when nothing written
                bufferWriter.Clear();

                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        $"Did not unwrap the message (NativeID: {messageContext.NativeMessageId} using {envelopeHandler.GetType().Name}");
                }
            }
            catch (Exception e)
            {
                metrics.RecordEnvelopeUnwrappingError(messageContext, envelopeHandler, e, false);
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
            Log.Debug(envelopeHandlers.Length > 0 ? $"No envelope handler unwrapped the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format" : $"No envelope handler found for the current message (NativeID: {messageContext.NativeMessageId}, assuming the default NServiceBus format");
        }

        return new IncomingMessageHandle(GetDefaultIncomingMessage(messageContext));
    }

    internal readonly struct IncomingMessageHandle(IncomingMessage message, IDisposable? disposable = null) : IDisposable
    {
        public IncomingMessage Message { get; } = message;

        public void Dispose() => disposable?.Dispose();

        public static implicit operator IncomingMessage(IncomingMessageHandle handle) => handle.Message;
    }

    static readonly ILog Log = LogManager.GetLogger<EnvelopeUnwrapper>();
}