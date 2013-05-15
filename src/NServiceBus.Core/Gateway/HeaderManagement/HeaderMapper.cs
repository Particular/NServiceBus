namespace NServiceBus.Gateway.HeaderManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HeaderMapper
    {
        public static void Map(IDictionary<string,string> from, TransportMessage to)
        {
            to.Id = from[NServiceBus + Id];
            to.CorrelationId = from[NServiceBus + CorrelationId];

            bool recoverable;
            if(bool.TryParse(from[NServiceBus + Recoverable], out recoverable))
                to.Recoverable = recoverable;

            TimeSpan timeToBeReceived;
            TimeSpan.TryParse(from[NServiceBus + TimeToBeReceived], out timeToBeReceived);
            to.TimeToBeReceived = timeToBeReceived;

            if (to.TimeToBeReceived < TimeSpan.FromSeconds(1))
                to.TimeToBeReceived = TimeSpan.FromSeconds(1);

            foreach (string header in from.Keys)
                if (header.Contains(NServiceBus + Headers.HeaderName))
                    to.Headers[header.Replace(NServiceBus + Headers.HeaderName + ".", "")] =  from[header];
        }

        public static void Map(TransportMessage from, IDictionary<string,string> to)
        {
            to[NServiceBus + Id] = from.Id;
            to[NServiceBus + CorrelationId] = from.CorrelationId;
            to[NServiceBus + Recoverable] = from.Recoverable.ToString();
            to[NServiceBus + TimeToBeReceived] = from.TimeToBeReceived.ToString();

            to[NServiceBus + ReplyToAddress] = from.ReplyToAddress.ToString();
         
            if (from.Headers.ContainsKey(ReplyToAddress))
                to[Headers.RouteTo] = from.Headers[ReplyToAddress];

            from.Headers.ToList()
                .ForEach(header =>to[NServiceBus + Headers.HeaderName + "." + header.Key] = header.Value);
        }

        public const string NServiceBus = "NServiceBus.";
        public const string Id = "Id";
        public const string CallType = "CallType";
        public const string DATABUS_PREFIX = "NServiceBus.DataBus.";

        const string CorrelationId = "CorrelationId";
        const string Recoverable = "Recoverable";
        const string ReplyToAddress = "ReplyToAddress";
        const string TimeToBeReceived = "TimeToBeReceived";
    }
}
