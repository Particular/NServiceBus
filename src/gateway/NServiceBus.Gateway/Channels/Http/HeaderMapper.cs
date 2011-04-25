namespace NServiceBus.Gateway.Channels.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unicast.Transport;
    using System.Collections.Specialized;

    public class HeaderMapper
    {
        public static void Map(NameValueCollection from, TransportMessage to)
        {
            to.Id = from[NServiceBus + Id];
            to.IdForCorrelation = from[NServiceBus + IdForCorrelation];
            to.CorrelationId = from[NServiceBus + CorrelationId];

            bool recoverable;
            bool.TryParse(from[NServiceBus + Recoverable], out recoverable);
            to.Recoverable = recoverable;

            TimeSpan timeToBeReceived;
            TimeSpan.TryParse(from[NServiceBus + TimeToBeReceived], out timeToBeReceived);
            to.TimeToBeReceived = timeToBeReceived;

            to.Headers = new Dictionary<string, string>();
            foreach (string header in from.Keys)
                if (header.Contains(NServiceBus + Headers.HeaderName))
                    to.Headers.Add(header.Replace(NServiceBus + Headers.HeaderName + ".", ""), from[header]);
        }

        public static void Map(TransportMessage from, NameValueCollection to)
        {
            //todo - why are we doing this?
            if (!String.IsNullOrEmpty(from.IdForCorrelation))
                from.IdForCorrelation = from.Id;

            to[NServiceBus + Id] = from.Id;
            to[NServiceBus + IdForCorrelation] = from.IdForCorrelation;
            to[NServiceBus + CorrelationId] = from.CorrelationId;
            to[NServiceBus + Recoverable] = from.Recoverable.ToString();
            to[NServiceBus + TimeToBeReceived] = from.TimeToBeReceived.ToString();

            to[NServiceBus + ReturnAddress] = from.ReturnAddress;
            to[NServiceBus + Headers.HeaderName + "." + ReturnAddress] = from.ReturnAddress;

            if (from.Headers.ContainsKey(ReturnAddress))
                to[Headers.RouteTo] = from.Headers[ReturnAddress];

            from.Headers.ToList()
                .ForEach(header =>to[NServiceBus + Headers.HeaderName + "." + header.Key] = header.Value);
        }

        public const string NServiceBus = "NServiceBus.";
        public const string Id = "Id";
        public const string CallType = "CallType";
        private const string IdForCorrelation = "IdForCorrelation";
        private const string CorrelationId = "CorrelationId";
        private const string Recoverable = "Recoverable";
        private const string ReturnAddress = "ReturnAddress";
        private const string TimeToBeReceived = "TimeToBeReceived";
    }
}
