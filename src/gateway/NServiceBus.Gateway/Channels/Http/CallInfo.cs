namespace NServiceBus.Gateway.Channels.Http
{
    using System.Collections.Specialized;

    public class CallInfo
    {
        public string ClientId { get; set; }
        public CallType Type { get; set; }
        public NameValueCollection Headers { get; set; }
        public byte[] Buffer { get; set; }
    }

    public enum CallType
    {
        Submit,
        Ack,
        DatabusProperty
    }
}
