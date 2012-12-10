namespace NServiceBus.Gateway.Tests
{
    using System.Collections.Generic;
    using System.IO;
    using System;
    using DataBus;

    //todo add this to the databus project
	public class InMemoryDataBus:IDataBus
    {
        readonly IDictionary<string,Entry> storage = new Dictionary<string, Entry>();
        
        public Stream Get(string key)
        {
            lock(storage)
                return new MemoryStream(storage[key].Data);
        }

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
        	var key = Guid.NewGuid().ToString();

            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
            
            lock(storage)
                storage.Add(key,new Entry
                                    {
                                        Data = data,
                                        ExpireAt = DateTime.Now + timeToBeReceived
                                    });
        	return key;
        }

		public void Start()
		{
			//no-op
		}

		public void Dispose()
		{
			//no-op
		}

        //used for test purposes
        public Entry Peek(string key)
        {
            lock (storage)
                return storage[key];
        }

        public class Entry
        {
            public byte[] Data;
            public DateTime ExpireAt;
        }
    }
}