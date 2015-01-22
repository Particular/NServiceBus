namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;

    class OutgoingHeaders
    {
        readonly Dictionary<object, Dictionary<string, string>> headers = new Dictionary<object, Dictionary<string, string>>(); 

        public void Add(object message, string header, string value)
        {
            Dictionary<string, string> thisMessageHeaders;
            if (!headers.TryGetValue(message, out thisMessageHeaders))
            {
                thisMessageHeaders = new Dictionary<string, string>();
                headers[message] = thisMessageHeaders;
            }
            thisMessageHeaders[header] = value;
        }

        public string TryGet(object message, string header)
        {
            Dictionary<string, string> thisMessageHeaders;
            string value;
            if (headers.TryGetValue(message, out thisMessageHeaders)
                && thisMessageHeaders.TryGetValue(header, out value))
            {
                return value;
            }
            return null;
        }

        public Dictionary<string, string> GetAndRemoveAll(object message)
        {
            Dictionary<string, string> thisMessageHeaders;
            if (headers.TryGetValue(message, out thisMessageHeaders))
            {
                headers.Remove(message);
                return thisMessageHeaders;
            }
            return new Dictionary<string, string>();
        }
    }
}