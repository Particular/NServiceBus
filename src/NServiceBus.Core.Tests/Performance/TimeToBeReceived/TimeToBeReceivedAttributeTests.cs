namespace NServiceBus.Core.Tests.Performance.TimeToBeReceived
{
    using System;
    using NUnit.Framework;

    public class TimeToBeReceivedAttributeTests
    {
        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree_when_initialized()
        {
            var mappings = new TimeToBeReceivedMappings(new[]
            {
                typeof(InheritedClassWithAttribute)
            }, TimeToBeReceivedMappings.DefaultConvention, true);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithAttribute), out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceived);
        }

        [Test]
        public void Should_use_inherited_TimeToBeReceived_when_initialized()
        {
            var mappings = new TimeToBeReceivedMappings(new[]
            {
                typeof(InheritedClassWithNoAttribute)
            }, TimeToBeReceivedMappings.DefaultConvention, true);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithNoAttribute), out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceived);
        }

        [Test]
        public void Should_throw_when_discard_before_received_not_supported_when_initialized()
        {
            Assert.That(() => new TimeToBeReceivedMappings(new[]
            {
                typeof(InheritedClassWithNoAttribute)
            }, TimeToBeReceivedMappings.DefaultConvention, doesTransportSupportDiscardIfNotReceivedBefore: false), Throws.Exception.Message.StartWith("Messages with TimeToBeReceived found but the selected transport does not support this type of restriction"));
        }

        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree_when_tryget()
        {
            var mappings = new TimeToBeReceivedMappings(new Type[]
            {
            }, TimeToBeReceivedMappings.DefaultConvention, true);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithAttribute), out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceived);
        }

        [Test]
        public void Should_use_inherited_TimeToBeReceived_when_tryget()
        {
            var mappings = new TimeToBeReceivedMappings(new Type[]
            {
            }, TimeToBeReceivedMappings.DefaultConvention, true);

            TimeSpan timeToBeReceived;

            Assert.True(mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithNoAttribute), out timeToBeReceived));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceived);
        }

        [Test]
        public void Should_throw_when_discard_before_received_not_supported_when_tryget()
        {
            var mappings = new TimeToBeReceivedMappings(new Type[]
            {
            }, TimeToBeReceivedMappings.DefaultConvention, doesTransportSupportDiscardIfNotReceivedBefore: false);

            Assert.That(
                () => mappings.TryGetTimeToBeReceived(typeof(InheritedClassWithNoAttribute), out _), 
                Throws.Exception.Message.StartWith("Messages with TimeToBeReceived found but the selected transport does not support this type of restriction"));
        }

        [TimeToBeReceived("00:00:01")]
        class BaseClass
        {
        }

        [TimeToBeReceived("00:00:02")]
        class InheritedClassWithAttribute : BaseClass
        {
        }

        class InheritedClassWithNoAttribute : BaseClass
        {
        }
    }
}