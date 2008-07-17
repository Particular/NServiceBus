using System;
using System.Collections;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast
{
    public class HeaderAdapter : IDictionary<string, string>
    {
        private readonly List<HeaderInfo> headers = new List<HeaderInfo>();

        public HeaderAdapter(List<HeaderInfo> headers)
        {
            if (headers != null)
                this.headers = headers;
        }

        public static List<HeaderInfo> From(IDictionary<string, string> source)
        {
            List<HeaderInfo> result = new List<HeaderInfo>(source.Count);

            foreach(string key in source.Keys)
                result.Add(new HeaderInfo(key, source[key]));

            return result;
        }

        #region IDictionary<string,string> Members

        public void Add(string key, string value)
        {
            foreach (HeaderInfo header in headers)
                if (header.Key == key)
                    throw new ArgumentException("An element with the same key already exists.");

            headers.Add(new HeaderInfo(key, value));
        }

        public bool ContainsKey(string key)
        {
            foreach (HeaderInfo header in headers)
                if (header.Key == key)
                    return true;

            return false;
        }

        public ICollection<string> Keys
        {
            get
            {
                List<string> result = new List<string>(headers.Count);
                foreach(HeaderInfo header in headers)
                    result.Add(header.Key);

                return result.AsReadOnly();
            }
        }

        public bool Remove(string key)
        {
            int index = -1;
            for (int i = 0; i < headers.Count; i++)
                if (headers[i].Key == key)
                    index = i;

            if (index != -1)
            {
                headers.RemoveAt(index);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out string value)
        {
            foreach (HeaderInfo header in headers)
                if (header.Key == key)
                {
                    value = header.Value;
                    return true;
                }

            value = null;
            return false;
        }

        public ICollection<string> Values
        {
            get
            {
                List<string> result = new List<string>(headers.Count);
                foreach (HeaderInfo header in headers)
                    result.Add(header.Value);

                return result.AsReadOnly();
            }
        }

        public string this[string key]
        {
            get
            {
                foreach (HeaderInfo header in headers)
                    if (header.Key == key)
                        return header.Value;

                return null;
            }
            set
            {
                for(int i=0; i < headers.Count; i++)
                    if (headers[i].Key == key)
                    {
                        headers[i] = new HeaderInfo(key, value);
                        return;
                    }

                Add(key, value);
            }
        }

        #endregion

        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            headers.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        public int Count
        {
            get { return headers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach(HeaderInfo header in headers)
                yield return new KeyValuePair<string, string>(header.Key, header.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return headers.GetEnumerator();
        }
    }
}
