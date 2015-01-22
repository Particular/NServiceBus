namespace NServiceBus.Core.Tests.SecondLevelRetries
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.SecondLevelRetries;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultRetryPolicyTests
    {
        [Test]
        public void ShouldRetryTheSpecifiedTimesWithIncreasedDelay()
        {
            var baseDelay = TimeSpan.FromSeconds(10);

            var policy = new DefaultSecondLevelRetryPolicy(2, baseDelay);
            TimeSpan delay;

            Assert.True(policy.TryGetDelay(new TransportMessage("",new Dictionary<string, string>()),new Exception(""),1,out delay));
            Assert.AreEqual(baseDelay,delay);
            Assert.True(policy.TryGetDelay(new TransportMessage("", new Dictionary<string, string>()), new Exception(""), 2, out delay));
            Assert.AreEqual(TimeSpan.FromSeconds(20), delay);
            Assert.False(policy.TryGetDelay(new TransportMessage("", new Dictionary<string, string>()), new Exception(""), 3, out delay));
        }


        [Test]
        public void ShouldCapTheRetryMaxTimeTo24Hours()
        {
            var baseDelay = TimeSpan.FromSeconds(10);

            var policy = new DefaultSecondLevelRetryPolicy(2, baseDelay);
            TimeSpan delay;

            var headers = new Dictionary<string, string>
            {
                {SecondLevelRetriesBehavior.RetriesTimestamp,DateTimeExtensions.ToWireFormattedString(DateTime.Now.AddHours(-24))}
            };

            Assert.False(policy.TryGetDelay(new TransportMessage("", headers), new Exception(""), 1, out delay));
        
        }
    }

    
}
