using System;
using System.Collections;
using System.Collections.Generic;
using NServiceBus.Unicast.Transport;

namespace NServiceBus.Unicast
{
    /// <summary>
    /// Serves as a go-between from an IDictionary{string, string} 
    /// used by application developers and List{HeaderInfo} used
    /// by the infrastructure.
    /// </summary>
    public class HeaderAdapter : IDictionary<string, string>
    {
        private readonly List<HeaderInfo> headers = new List<HeaderInfo>();

        /// <summary>
        /// Creates a new instance storing the given headers.
        /// </summary>
        /// <param name="headers"></param>
        public HeaderAdapter(List<HeaderInfo> headers)
        {
            if (headers != null)
                this.headers = headers;
        }

        /// <summary>
        /// Returns a strongly type list of HeaderInfo from the given dictionary.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<HeaderInfo> From(IDictionary<string, string> source)
        {
            List<HeaderInfo> result = new List<HeaderInfo>();

            if (source != null)
                foreach(string key in source.Keys)
                    result.Add(new HeaderInfo { Key = key, Value = source[key] });

            return result;
        }

        #region IDictionary<string,string> Members

        /// <summary>
        /// Adds a new key,value pair.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            foreach (HeaderInfo header in headers)
                if (header.Key == key)
                    throw new ArgumentException("An element with the same key already exists.");

            headers.Add(new HeaderInfo { Key = key, Value = value });
        }

        /// <summary>
        /// Returns true of the key was previously added,
        /// otherwise false.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            foreach (HeaderInfo header in headers)
                if (header.Key == key)
                    return true;

            return false;
        }

        /// <summary>
        /// Returns the collection of keys.
        /// </summary>
        public ICollection<string> Keys
        {
            get
            {
                List<string> result = new List<string>(headers.Count);
                foreach(HeaderInfo header in headers)
                    result.Add(header.Key);

                return result;
            }
        }

        /// <summary>
        /// Removes the entry under the given key returning true if found,
        /// otherwise false.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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

        /// <summary>
        /// If the key exists in the collection, the given value is put in the out parameter.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Returns the collection of values.
        /// </summary>
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

        /// <summary>
        /// Gets the value for the given key, or null if the key
        /// could not be found.
        /// 
        /// Sets the value for the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
                        headers[i] = new HeaderInfo { Key = key, Value = value };
                        return;
                    }

                Add(key, value);
            }
        }

        #endregion

        /// <summary>
        /// Adds the key value pair.
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clears all headers.
        /// </summary>
        public void Clear()
        {
            headers.Clear();
        }

        /// <summary>
        /// Returns a Contains on the key of the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<string, string> item)
        {
            return ContainsKey(item.Key);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls Remove on the key of the given item.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<string, string> item)
        {
            return Remove(item.Key);
        }

        /// <summary>
        /// Gets the number of elements actually contained.
        /// </summary>
        public int Count
        {
            get { return headers.Count; }
        }

        /// <summary>
        /// Returns false.
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a strongly typed enumerator for iterating over the collection.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            foreach(HeaderInfo header in headers)
                yield return new KeyValuePair<string, string>(header.Key, header.Value);
        }

        /// <summary>
        /// Gets an enumerator for iterating over the collection.
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return headers.GetEnumerator();
        }
    }
}
