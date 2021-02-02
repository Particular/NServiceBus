namespace NServiceBus.DataBus.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class InMemoryDataBus : IDataBus
    {
        IDictionary<string, Entry> storage = new Dictionary<string, Entry>();

        /// <summary>
        /// Gets a data item from the bus.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>The data <see cref="Stream"/>.</returns>
        public Task<Stream> Get(string key)
        {
            lock (storage)
            {
                return Task.FromResult((Stream)new MemoryStream(storage[key].Data));
            }
        }

        /// <summary>
        /// Adds a data item to the bus and returns the assigned key.
        /// </summary>
        /// <param name="stream">A create containing the data to be sent on the databus.</param>
        /// <param name="timeToBeReceived">The time to be received specified on the message type. TimeSpan.MaxValue is the default.</param>
        public Task<string> Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = Guid.NewGuid().ToString();

            var data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);

            lock (storage)
            {
                storage.Add(key, new Entry
                {
                    Data = data,
                    ExpireAt = DateTime.Now + timeToBeReceived
                });
            }

            return Task.FromResult(key);
        }

        /// <summary>
        /// Called when the bus starts up to allow the data bus to active background tasks.
        /// </summary>
        public Task Start()
        {
            //no-op
            return TaskEx.CompletedTask;
        }

        //used for test purposes
        public Entry Peek(string key)
        {
            lock (storage)
            {
                return storage[key];
            }
        }

        public class Entry
        {
            public byte[] Data;
// ReSharper disable NotAccessedField.Global
            public DateTime ExpireAt;
// ReSharper restore NotAccessedField.Global
        }
    }
}