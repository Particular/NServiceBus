using System.Collections.Generic;
using System.IO;

namespace NServiceBus.DataBus.Tests
{
	using System;

	public class InMemoryDataBus:IDataBus
    {
        readonly IDictionary<string,byte[]> storage = new Dictionary<string, byte[]>();
        
        public Stream Get(string key)
        {
            return new MemoryStream(storage[key]);
        }

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
        	var key = Guid.NewGuid().ToString();

            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            storage.Add(key,data);
        	return key;
        }
    }
}