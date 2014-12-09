namespace NServiceBus.Serializers.XML.Test
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class UriTests
    {
        [Test]
        public void Should_support()
        {
            var expected = new Uri(@"http://docs.google.com/uc?authuser=1&id=0BzGD5JpB16DVTWNoemYyNkY3ZEk&ex");

            var result = ExecuteSerializer.ForMessage<MessageWithUri>(m3 =>
            {
                m3.Href = expected;
            });

            Assert.AreEqual(expected, result.Href);
        }

        public class MessageWithUri : ICommand
        {
            public Uri Href { get; set; }
        }
    }
}
