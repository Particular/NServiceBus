namespace NServiceBus.Pipeline.Contexts
{
    using System.Collections.Generic;

    class OutgoingHeaders
    {
        Dictionary<object, Dictionary<string, string>> headers;

        public void Add(object message, string header, string value)
        {
            if (headers == null)
            {
                headers = new Dictionary<object, Dictionary<string, string>>
                {
                    {
                        message, new Dictionary<string, string>
                        {
                            {header, value}
                        }
                    }
                };
                return;
            }

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

        public Dictionary<string, string> GetAndRemove(object message)
        {
            if (headers == null)
            {
                return new Dictionary<string, string>();
            }

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