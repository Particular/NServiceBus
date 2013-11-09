namespace NServiceBus.Core.Tests.Licensing
{
    using System;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
	public class TimestampTests
	{
		[Test]
		public void Ensure_timestamp_is_embedded()
		{
		    var dateTime = TimestampReader.GetBuildTimestamp();
            Assert.AreEqual(DateTime.UtcNow.Date,dateTime.Date);
		}
	}
}
