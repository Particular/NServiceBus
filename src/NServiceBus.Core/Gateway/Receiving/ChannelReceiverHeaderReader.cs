namespace NServiceBus.Gateway.Receiving
{
    using System;
    using System.Collections.Generic;
    using Channels;
    using Channels.Http;
    using HeaderManagement;
    using Sending;

    static class ChannelReceiverHeaderReader
    {
        
        internal static CallInfo GetCallInfo(DataReceivedOnChannelArgs receivedData)
        {
            return new CallInfo
                {
                    ClientId = ReadClientId(receivedData.Headers),
                    TimeToBeReceived = ReadTimeToBeReceived(receivedData.Headers),
                    Type = ReadCallType(receivedData.Headers),
                    Headers = receivedData.Headers,
                    Data = receivedData.Data,
                    AutoAck = receivedData.Headers.ContainsKey(GatewayHeaders.AutoAck),
                    Md5 = ReadMd5(receivedData.Headers)
                };
        }

        static TimeSpan ReadTimeToBeReceived(IDictionary<string, string> headers)
        {
            string timeToBeReceivedString;
            if (headers.TryGetValue("NServiceBus.TimeToBeReceived", out timeToBeReceivedString))
            {
                TimeSpan timeToBeReceived;
                if (TimeSpan.TryParse(timeToBeReceivedString, out timeToBeReceived))
                {
                    return timeToBeReceived;
                }
            }
            return TimeSpan.FromHours(1);
        }

        internal static string ReadMd5(IDictionary<string, string> headers)
        {
            string md5;
            headers.TryGetValue(HttpHeaders.ContentMd5Key, out md5);

            if (string.IsNullOrWhiteSpace(md5))
            {
                throw new ChannelException(400, "Required header '" + HttpHeaders.ContentMd5Key + "' missing.");
            }
            return md5;
        }
        internal static string ReadDataBus(this CallInfo callInfo)
        {
            string dataBus;
            callInfo.Headers.TryGetValue(GatewayHeaders.DatabusKey, out dataBus);

            if (string.IsNullOrWhiteSpace(dataBus))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.DatabusKey + "' missing.");
            }
            return dataBus;
        }

        internal static string ReadClientId(IDictionary<string, string> headers)
        {
            string clientIdString;
            headers.TryGetValue(GatewayHeaders.ClientIdHeader, out clientIdString);
            if (string.IsNullOrWhiteSpace(clientIdString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.ClientIdHeader + "' missing.");
            }
            return clientIdString;
        }

        internal static CallType ReadCallType(IDictionary<string, string> headers)
        {
            string callTypeString;
            CallType callType;
            if (!headers.TryGetValue(GatewayHeaders.CallTypeHeader, out callTypeString))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");
            }
            if (!Enum.TryParse(callTypeString, out callType))
            {
                throw new ChannelException(400, "Required header '" + GatewayHeaders.CallTypeHeader + "' missing.");
            }
            return callType;
        }
    }
}