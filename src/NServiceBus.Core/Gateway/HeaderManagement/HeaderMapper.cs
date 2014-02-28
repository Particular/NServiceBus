namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Transports.Msmq;

    public class HeaderMapper
    {
        public static TransportMessage Map(IDictionary<string, string> from)
        {
            if (!from.ContainsKey(GatewayHeaders.IsGatewayMessage))
            {
                var message = new TransportMessage();
                foreach (var header in from)
                {
                    message.Headers[header.Key] = header.Value;
                }

                return message;
            }

            var headers = ExtractHeaders(from);
            var to = new TransportMessage(from[NServiceBus + Id], headers);

            to.CorrelationId = StripSlashZeroFromCorrelationId(from[NServiceBus + CorrelationId]) ?? to.Id;

            bool recoverable;
            if (bool.TryParse(from[NServiceBus + Recoverable], out recoverable))
            {
                to.Recoverable = recoverable;
            }

            TimeSpan timeToBeReceived;
            TimeSpan.TryParse(from[NServiceBus + TimeToBeReceived], out timeToBeReceived);
            to.TimeToBeReceived = timeToBeReceived;

            if (to.TimeToBeReceived < MinimumTimeToBeReceived)
            {
                to.TimeToBeReceived = MinimumTimeToBeReceived;
            }

            return to;
        }

        static Dictionary<string, string> ExtractHeaders(IDictionary<string, string> from)
        {
            var result = new Dictionary<string, string>();

            foreach (var pair in from)
            {
                if (pair.Key.Contains(NServiceBus + Headers.HeaderName))
                {
                    result.Add(pair.Key.Replace(NServiceBus + Headers.HeaderName + ".", String.Empty), pair.Value);
                }
            }

            return result;
        }

        public static void Map(TransportMessage from, IDictionary<string, string> to)
        {
            to[NServiceBus + Id] = from.Id;
            to[NServiceBus + CorrelationId] = GetCorrelationForBackwardsCompatibility(from);
            to[NServiceBus + Recoverable] = from.Recoverable.ToString();
            to[NServiceBus + TimeToBeReceived] = from.TimeToBeReceived.ToString();

            if (from.ReplyToAddress != null) //Handles SendOnly endpoints, where ReplyToAddress is not set
            {
                to[NServiceBus + ReplyToAddress] = from.ReplyToAddress.ToString();
            }

            SetBackwardsCompatibilityHeaders(to);

            string replyToAddress;
            if (from.Headers.TryGetValue(ReplyToAddress, out replyToAddress))
            {
                to[Headers.RouteTo] = replyToAddress;
            }

            from.Headers.ToList()
                .ForEach(header => to[NServiceBus + Headers.HeaderName + "." + header.Key] = header.Value);
        }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        static string StripSlashZeroFromCorrelationId(string corrId)
        {
            if (corrId == null)
            {
                return null;
            }

            if (Configure.HasComponent<MsmqMessageSender>())
            {
                if (corrId.EndsWith("\\0"))
                {
                    return corrId.Replace("\\0", String.Empty);
                }
            }

            return corrId;
        }

        [ObsoleteEx(RemoveInVersion = "5.1", TreatAsErrorFromVersion = "5.0")]
        static void SetBackwardsCompatibilityHeaders(IDictionary<string, string> to)
        {
            if (Configure.HasComponent<MsmqMessageSender>())
            {
                to[NServiceBus + IdForCorrelation] = to[NServiceBus + CorrelationId];
            }
        }

        [ObsoleteEx(RemoveInVersion = "5.0")]
        static string GetCorrelationForBackwardsCompatibility(TransportMessage message)
        {
            var correlationIdToStore = message.CorrelationId;

            if (Configure.HasComponent<MsmqMessageSender>())
            {
                Guid correlationId;

                if (Guid.TryParse(message.CorrelationId, out correlationId))
                {
                    correlationIdToStore = message.CorrelationId + "\\0";
                        //msmq required the id's to be in the {guid}\{incrementing number} format so we need to fake a \0 at the end to make it compatible                
                }
            }

            return correlationIdToStore;
        }

        public const string NServiceBus = "NServiceBus.";
        public const string Id = "Id";
        public const string CallType = "CallType";
        public const string DATABUS_PREFIX = "NServiceBus.DataBus.";

        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string ReplyToAddress = "ReplyToAddress";
        const string TimeToBeReceived = "TimeToBeReceived";
        const string IdForCorrelation = "IdForCorrelation";
        static readonly TimeSpan MinimumTimeToBeReceived = TimeSpan.FromSeconds(1);
    }
}
