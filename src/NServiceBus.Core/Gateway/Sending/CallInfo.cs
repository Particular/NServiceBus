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

        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public bool AutoAck { get; set; }
    }

    public enum CallType
    {
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        LegacySubmit,
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        LegacyAck,
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        LegacyDatabusProperty,

        Submit,
        DatabusProperty
    }
}
