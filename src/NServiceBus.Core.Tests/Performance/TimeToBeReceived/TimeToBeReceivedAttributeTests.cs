namespace NServiceBus.Core.Tests.Performance.TimeToBeReceived
{
    using System;
    using NUnit.Framework;

    public class TimeToBeReceivedAttributeTests
    {
        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree()
        {
            var mappings = new TimeToBeReceivedMappings(new[]{typeof(InheritedClassWithAttribute)},TimeToBeReceivedMappings.DefaultConvention);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithAttribute),out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceived);
        }

        [Test]
        public void Should_use_inherited_TimeToBeReceived()
        {
            var mappings = new TimeToBeReceivedMappings(new[] { typeof(InheritedClassWithNoAttribute) }, TimeToBeReceivedMappings.DefaultConvention);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithNoAttribute), out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceived);
        }

        [TimeToBeReceivedAttribute("00:00:01")]
        class BaseClass
        {
        }

        [TimeToBeReceivedAttribute("00:00:02")]
        class InheritedClassWithAttribute : BaseClass
        {

        }

        class InheritedClassWithNoAttribute : BaseClass
        {

        }
    }
}