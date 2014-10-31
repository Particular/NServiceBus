namespace NServiceBus.Management.Retries.Tests
{
    using System;
    using SecondLevelRetries;
    using SecondLevelRetries.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class SecondLevelRetriesProcessorTests
    {
        readonly int[] _expectedResults = new[]
        {
            10,
            20,
            30
        };

        TransportMessage _message;

        [SetUp]
        public void SetUp()
        {
            _message = new TransportMessage();
        }

        [Test]
        public void The_time_span_should_increase_with_10_sec_for_every_retry()
        {
            var retriesProcessor = new SecondLevelRetriesProcessor
            {
                SecondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration()
            };

            for (var i = 0; i < 3; i++)
            {
                var timeSpan = retriesProcessor.SecondLevelRetriesConfiguration.RetryPolicy(_message);

                Defer();

                Assert.AreEqual(_expectedResults[i], timeSpan.Seconds);
            }
        }

        [Test]
        public void The_default_time_out_should_be_1_day()
        {
            TransportMessageHeaderHelper.SetHeader(_message, SecondLevelRetriesHeaders.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow.AddDays(-1).AddSeconds(-1)));

            var retriesProcessor = new SecondLevelRetriesProcessor
            {
                SecondLevelRetriesConfiguration = new SecondLevelRetriesConfiguration()
            };

            var hasTimedOut = retriesProcessor.SecondLevelRetriesConfiguration.RetryPolicy(_message) == TimeSpan.MinValue;
            Assert.IsTrue(hasTimedOut);
        }

        void Defer()
        {
            TransportMessageHeaderHelper.SetHeader(_message, Headers.Retries, (TransportMessageHeaderHelper.GetNumberOfRetries(_message) + 1).ToString());
        }
    }
}