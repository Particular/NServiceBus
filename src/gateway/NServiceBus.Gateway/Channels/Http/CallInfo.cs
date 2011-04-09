namespace NServiceBus.Gateway.Channels.Http
{
    public class CallInfo
    {
        public string ClientId { get; set; }
        public string MD5 { get; set; }
        public CallType Type { get; set; }
    }

    public enum CallType
    {
        Submit,
        Ack,
        DatabusProperty
    }
}
