namespace NServiceBus.Core.Tests.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultRetryPolicyTests
    {
        [Test]
        public void ShouldRetryTheSpecifiedTimesWithIncreasedDelay()
        {
            var message = new IncomingMessage("someid", new Dictionary<string, string>(), Stream.Null);
            var baseDelay = TimeSpan.FromSeconds(10);

            var policy = new DefaultSecondLevelRetryPolicy(2, baseDelay);
            TimeSpan delay;

            Assert.True(policy.TryGetDelay(new SecondLevelRetryContext { Message = message, SecondLevelRetryAttempt = 1 }, out delay));
            Assert.AreEqual(baseDelay, delay);
            Assert.True(policy.TryGetDelay(new SecondLevelRetryContext { Message = message, SecondLevelRetryAttempt = 2 }, out delay));
            Assert.AreEqual(TimeSpan.FromSeconds(20), delay);
            Assert.False(policy.TryGetDelay(new SecondLevelRetryContext { Message = message, SecondLevelRetryAttempt = 3 }, out delay));
        }

        [Test]
        public void ShouldCapTheRetryMaxTimeTo24Hours()
        {
            var now = DateTime.UtcNow;
            var baseDelay = TimeSpan.FromSeconds(10);

            var policy = new DefaultSecondLevelRetryPolicy(2, baseDelay, () => now);
            TimeSpan delay;

            var moreThanADayAgo = now.AddHours(-24).AddTicks(-1);
            var headers = new Dictionary<string, string>
            {
                {Headers.RetriesTimestamp, DateTimeExtensions.ToWireFormattedString(moreThanADayAgo)}
            };
            var message = new IncomingMessage("someid", headers, Stream.Null);

            Assert.False(policy.TryGetDelay(new SecondLevelRetryContext { Message = message, SecondLevelRetryAttempt = 1 }, out delay));
        }
    }
}