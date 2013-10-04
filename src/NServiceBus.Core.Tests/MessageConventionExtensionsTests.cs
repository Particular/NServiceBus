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
            var timeToBeReceivedAction = MessageConventionExtensions.TimeToBeReceivedAction(typeof(InheritedClass));
            Assert.AreEqual(TimeSpan.FromSeconds(2), timeToBeReceivedAction);
        }

        [TimeToBeReceivedAttribute("00:00:01")]
        class BaseClass
        {
        }
        [TimeToBeReceivedAttribute("00:00:02")]
        class InheritedClass : BaseClass
        {
            
        }
    }

}