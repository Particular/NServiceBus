namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    class OutgoingMessageHeaders : DictionaryWrapper<string, string>
    {
        public OutgoingMessageHeaders(IDictionary<string, string> inner) : base(inner)
        {
        }

        public override string this[string key]
        {
            get { return base[key]; }
            set { base[CheckKey(key)] = value; }
        }

        public override void Add(KeyValuePair<string, string> item)
        {
            CheckKey(item.Key);
            base.Add(item);
        }

        public override void Add(string key, string value)
        {
            base.Add(CheckKey(key), value);
        }

        static string CheckKey(string key)
        {
            if(string.Equals(Headers.MessageId, key, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Setting Message Id by manipulating the `{Headers.MessageId}` header is not supported. Use `sendOptions.SetMessageId(...)` instead.");
            return key;
        }
    }
}