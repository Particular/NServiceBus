﻿namespace NServiceBus.Core.Tests.DataBus.FileShare
{
    using System;
    using System.IO;
    using System.Text;
    using NUnit.Framework;
    using NServiceBus.DataBus.FileShare;

    [TestFixture]
	public class AcceptanceTests
	{
		FileShareDataBus dataBus;
	    readonly string basePath = Path.GetTempPath();

		[SetUp]
		public void SetUp()
		{
			dataBus = new FileShareDataBus(basePath);
		    dataBus.MaxMessageTimeToLive = TimeSpan.MaxValue;
		}

		[Test]
		public void Should_handle_max_ttl()
		{
			Put("Test", TimeSpan.MaxValue);
			Assert.True(Directory.Exists(Path.Combine(basePath, DateTime.MaxValue.ToString("yyyy-MM-dd_HH"))));
		}

		[Test]
		public void Should_honour_the_ttl_limit()
		{
			dataBus.MaxMessageTimeToLive = TimeSpan.FromDays(1);

			Put("Test", TimeSpan.MaxValue);
			Assert.True(Directory.Exists(Path.Combine(basePath, DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd_HH"))));
		}

		[Test]
		public void Should_handle_be_able_to_read_stored_values()
		{
			const string content = "Test";

			var key = Put(content, TimeSpan.MaxValue);
			using(var stream =  dataBus.Get(key))
			{
				Assert.AreEqual(new StreamReader(stream).ReadToEnd(),content);			
			}
		}

		string Put(string content,TimeSpan timeToLive)
		{
			byte[] byteArray = Encoding.ASCII.GetBytes( content);
			using (var stream = new MemoryStream(byteArray))
				return dataBus.Put(stream, timeToLive);
		}
	}
}
