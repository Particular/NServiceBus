namespace NServiceBus.DataBus.FileShare
{
	using System;
	using System.IO;
	using DataBus;

	public class FileShareDataBus : IDataBus
	{
		readonly string basePath;

		public FileShareDataBus(string basePath)
		{
			this.basePath = basePath;
		}

		public TimeSpan MaxMessageTimeToLive { get; set; }

		public Stream Get(string key)
		{
			return new FileStream(Path.Combine(basePath, key), FileMode.Open);
		}

		public string Put(Stream stream, TimeSpan timeToBeReceived)
		{
			var key = GenerateKey(timeToBeReceived);

			var filePath = Path.Combine(basePath, key);

			Directory.CreateDirectory(Path.GetDirectoryName(filePath));

			using (var output = new FileStream(filePath, FileMode.CreateNew))
			{
				var buffer = new byte[32 * 1024];
				int read;

				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					output.Write(buffer, 0, read);
				}
			}
			return key;
		}

		string GenerateKey(TimeSpan timeToBeReceived)
		{
			if (timeToBeReceived > MaxMessageTimeToLive)
				timeToBeReceived = MaxMessageTimeToLive;

			var keepMessageUntil = DateTime.MaxValue;

			if (timeToBeReceived < TimeSpan.MaxValue)
				keepMessageUntil = DateTime.Now + timeToBeReceived;

			return Path.Combine(keepMessageUntil.ToString("yyyy-MM-dd_hh"), Guid.NewGuid().ToString());
		}

	}
}
