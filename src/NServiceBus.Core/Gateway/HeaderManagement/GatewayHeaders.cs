namespace NServiceBus.Gateway.HeaderManagement
{
    public class GatewayHeaders
    {
        public const string AutoAck = "NServiceBus.AutoAck";
        public const string DatabusKey = "NServiceBus.Gateway.DataBusKey";

        public const string IsGatewayMessage = "NServiceBus.Gateway";

        public const string CallTypeHeader = HeaderMapper.NServiceBus + HeaderMapper.CallType;

        public const string ClientIdHeader = HeaderMapper.NServiceBus + HeaderMapper.Id;
    }
}