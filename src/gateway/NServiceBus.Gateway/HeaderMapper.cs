using System;
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
            to.Recoverable = bool.Parse(from[NServiceBus + Recoverable]);
            to.ReturnAddress = from[NServiceBus + ReturnAddress];
            to.TimeToBeReceived = TimeSpan.Parse(from[NServiceBus + TimeToBeReceived]);
            to.WindowsIdentityName = from[NServiceBus + WindowsIdentityName];

            to.Headers = new System.Collections.Generic.List<HeaderInfo>();
            foreach (string header in from.Keys)
                if (header.Contains(NServiceBus + Header))
                    to.Headers.Add(new HeaderInfo { Key = header.Replace(NServiceBus + Header, ""), Value = from[header] });
        }

        public static void Map(TransportMessage from, NameValueCollection to)
        {
            to[NServiceBus + Id] = from.Id;
            to[NServiceBus + IdForCorrelation] = from.IdForCorrelation;
            to[NServiceBus + CorrelationId] = from.CorrelationId;
            to[NServiceBus + Recoverable] = from.Recoverable.ToString();
            to[NServiceBus + ReturnAddress] = from.ReturnAddress;
            to[NServiceBus + TimeToBeReceived] = from.TimeToBeReceived.ToString();
            to[NServiceBus + WindowsIdentityName] = from.WindowsIdentityName;

            from.Headers.ForEach((info) => to[NServiceBus + Header + info.Key] = info.Value);
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
    }
}
