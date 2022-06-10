using System;
using System.Collections.Generic;
using NServiceBus.Features;

namespace NServiceBus.Performance.Metrics
{
    static class Extensions
    {
        public static void ThrowIfSendOnly(this FeatureConfigurationContext context)
        {
            var isSendOnly = context.Settings.GetOrDefault<bool>("Endpoint.SendOnly");
            if (isSendOnly)
            {
                throw new Exception("Metrics are not supported on send only endpoints.");
            }
        }
        
        public static bool TryGetMessageType(this ReceivePipelineCompleted completed, out string processedMessageType)
        {
            return completed.ProcessedMessage.Headers.TryGetMessageType(out processedMessageType);
        }

        internal static bool TryGetMessageType(this IReadOnlyDictionary<string, string> headers, out string processedMessageType)
        {
            if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageType))
            {
                processedMessageType = enclosedMessageType;
                return true;
            }
            processedMessageType = null;
            return false;
        }
    }
}