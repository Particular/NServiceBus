using System;

namespace NServiceBus.Unicast.Transport
{
    [Serializable]
    public class HeaderInfo
    {
        public string Key;
        public string Value;

        public HeaderInfo(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public HeaderInfo()
        {
        }
    }
}
