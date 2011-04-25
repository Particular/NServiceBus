namespace NServiceBus.Gateway.HeaderManagement
{
    public class GatewayReturnInfo
    {
        public string From { get; set; }
        public string To { get; set; } //todo - not used, check with Udi?
        public string ReturnAddress { get; set; }
    }
}