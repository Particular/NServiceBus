using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus.Unicast.Transport;
using System.Collections.Specialized;

namespace NServiceBus.Gateway
{
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
                if (header.Contains(NServiceBus + Header))
                    to.Headers.Add(header.Replace(NServiceBus + Header, ""), from[header] );
        }

        public static void Map(TransportMessage from, NameValueCollection to)
        {
            to[NServiceBus + Id] = from.Id;
            to[NServiceBus + IdForCorrelation] = from.IdForCorrelation;
            to[NServiceBus + CorrelationId] = from.CorrelationId;
            to[NServiceBus + Recoverable] = from.Recoverable.ToString();
            to[NServiceBus + TimeToBeReceived] = from.TimeToBeReceived.ToString();

            to[NServiceBus + ReturnAddress] = from.ReturnAddress;
            to[NServiceBus + Header + ReturnAddress] = from.ReturnAddress;

            if (from.Headers.ContainsKey(ReturnAddress))
                to[NServiceBus + Header + RouteTo] = from.Headers[ReturnAddress];

            from.Headers.ToList().ForEach(info => to[NServiceBus + Header + info.Key] = info.Value);
        }

        private const string NServiceBus = "NServiceBus.";
        private const string Id = "Id";
        private const string IdForCorrelation = "IdForCorrelation";
        private const string CorrelationId = "CorrelationId";
        private const string Recoverable = "Recoverable";
        private const string ReturnAddress = "ReturnAddress";
        private const string TimeToBeReceived = "TimeToBeReceived";
        private const string WindowsIdentityName = "WindowsIdentityName";
        private const string Header = "Header.";

        public const string RouteTo = "RouteTo";
    }

    public static class Headers
    {
        public const string ContentMd5Key = "Content-MD5";
        public const string FromKey = "From";
    }
}
