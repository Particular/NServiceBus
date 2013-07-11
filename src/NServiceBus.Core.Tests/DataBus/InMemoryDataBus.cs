namespace NServiceBus.Core.Tests.DataBus
{
    using System.Collections.Generic;
    using System.IO;
    using System;
    using NServiceBus.DataBus;

    public class InMemoryDataBus : IDataBus
    {
        private readonly IDictionary<string, byte[]> storage = new Dictionary<string, byte[]>();

        public Stream Get(string key)
        {
            lock (storage)
                return new MemoryStream(storage[key]);
        }

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = Guid.NewGuid().ToString();

            var data = new byte[stream.Length];
            stream.Read(data, 0, (int) stream.Length);

            lock (storage)
                storage.Add(key, data);
            return key;
        }

        public void Start()
        {
            //no-op
        }
    }
}