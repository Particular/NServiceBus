namespace NServiceBus.DataBus.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// In memory implementation of <see cref="IDataBus"/>.
    /// </summary>
    public class InMemoryDataBus : IDataBus
    {
        private readonly IDictionary<string, Entry> storage = new Dictionary<string, Entry>();

        /// <summary>
        /// Gets a data item from the bus.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>The data <see cref="Stream"/>.</returns>
        public Stream Get(string key)
        {
            lock (storage)
                return new MemoryStream(storage[key].Data);
        }

        /// <summary>
        /// Adds a data item to the bus and returns the assigned key.
        /// </summary>
        /// <param name="stream">A create containing the data to be sent on the databus.</param>
        /// <param name="timeToBeReceived">The time to be received specified on the message type. TimeSpan.MaxValue is the default.</param>
        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = Guid.NewGuid().ToString();

            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            lock (storage)
                storage.Add(key, new Entry
                {
                    Data = data,
                    ExpireAt = DateTime.Now + timeToBeReceived
                });
            return key;
        }

        /// <summary>
        /// Called when the bus starts up to allow the data bus to active background tasks.
        /// </summary>
        public void Start()
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
// ReSharper disable once NotAccessedField.Global
            public DateTime ExpireAt;
        }
    }
}