namespace NServiceBus.Gateway.Sending
{
    using System.Collections.Generic;
    using System.IO;

    public class CallInfo
    {
        public string ClientId { get; set; }
        public CallType Type { get; set; }
        public IDictionary<string,string> Headers { get; set; }
        public Stream Data { get; set; }
        public bool AutoAck { get; set; }
    }

    public enum CallType
    {
        Submit,
        Ack,
        DatabusProperty
    }
}
