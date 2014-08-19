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

            // the OriginalDate from ReleaseDateAttribute date should be less than now
            Assert.LessOrEqual(dateTime.Date, DateTime.UtcNow);
		}
	}
}
