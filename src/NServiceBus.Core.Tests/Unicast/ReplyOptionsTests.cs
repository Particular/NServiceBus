namespace NServiceBus.Unicast.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class ReplyOptionsTests
    {
        [Test]
        public void Should_throw_if_destination_is_null()
        {
            Assert.Throws<ArgumentNullException>(() => new ReplyOptions(null));
        }
    }
}