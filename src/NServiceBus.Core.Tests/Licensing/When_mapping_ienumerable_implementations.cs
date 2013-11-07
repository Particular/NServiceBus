namespace NServiceBus.Core.Tests.Licensing
{
    using System.Diagnostics;
    using NServiceBus.Licensing;
    using NUnit.Framework;

    [TestFixture]
	public class TimestampTests
	{
		[Test]
		public void Ensure_timestamp_is_embedded()
		{
		    var dateTimeOffset = TimestampReader.GetBuildTimestamp();
            Trace.WriteLine(dateTimeOffset);
		}
	}
}
