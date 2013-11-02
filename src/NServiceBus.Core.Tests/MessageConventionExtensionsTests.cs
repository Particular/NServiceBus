namespace NServiceBus.Core.Tests.Encryption
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MessageConventionExtensionsTests
    {
        [Test]
        public void Should_use_TimeToBeReceived_from_bottom_of_tree()
        {
            var timeToBeReceivedAction = MessageConventionExtensions.TimeToBeReceivedAction(typeof(InheritedClassWithAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceivedAction);
        }
        [Test]
        public void Should_use_inherited_TimeToBeReceived()
        {
            var timeToBeReceivedAction = MessageConventionExtensions.TimeToBeReceivedAction(typeof(InheritedClassWithNoAttribute));
            Assert.AreEqual(TimeSpan.FromSeconds(1), timeToBeReceivedAction);
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