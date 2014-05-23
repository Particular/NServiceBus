namespace NServiceBus.DataBus.FileShare
{
    using System;
    using System.IO;
    using Logging;

    /// <summary>
    /// File share implementation of <see cref="IDataBus"/>.
    /// </summary>
    public class FileShareDataBus : IDataBus
	{
		readonly string basePath;
        static ILog logger = LogManager.GetLogger < FileShareDataBus>();

		/// <summary>
        /// Create a <see cref="FileShareDataBus"/> with the specified <paramref name="basePath"/>.
		/// </summary>
		/// <param name="basePath">The path to save files on.</param>
		public FileShareDataBus(string basePath)
		{
			this.basePath = basePath;
		}

        /// <summary>
        /// Gets/Sets the maximum message TTL.
        /// </summary>
		public TimeSpan MaxMessageTimeToLive { get; set; }

        /// <summary>
        /// Gets a data item from the bus.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>The data <see cref="Stream"/>.</returns>
        public Stream Get(string key)
		{
            var filePath = Path.Combine(basePath, key);

            logger.DebugFormat("Opening stream from '{0}'.", filePath);

            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		}

        /// <summary>
        /// Adds a data item to the bus and returns the assigned key.
        /// </summary>
        /// <param name="stream">A create containing the data to be sent on the databus.</param>
        /// <param name="timeToBeReceived">The time to be received specified on the message type. TimeSpan.MaxValue is the default.</param>
        public string Put(Stream stream, TimeSpan timeToBeReceived)
		{
			var key = GenerateKey(timeToBeReceived);

			var filePath = Path.Combine(basePath, key);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
			{
				var buffer = new byte[32 * 1024];
				int read;

				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					output.Write(buffer, 0, read);
				}
			}

            logger.DebugFormat("Saved stream to '{0}'.", filePath);

			return key;
		}

        /// <summary>
        /// Called when the bus starts up to allow the data bus to active background tasks.
        /// </summary>
        public void Start()
		{
			logger.Info("File share data bus started. Location: " + basePath);

			//TODO: Implement a clean up thread
		}

		string GenerateKey(TimeSpan timeToBeReceived)
		{
			if (timeToBeReceived > MaxMessageTimeToLive)
				timeToBeReceived = MaxMessageTimeToLive;

			var keepMessageUntil = DateTime.MaxValue;

			if (timeToBeReceived < TimeSpan.MaxValue)
				keepMessageUntil = DateTime.Now + timeToBeReceived;

			return Path.Combine(keepMessageUntil.ToString("yyyy-MM-dd_HH"), Guid.NewGuid().ToString());
		}
	}
}
