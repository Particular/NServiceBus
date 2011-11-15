

namespace NServiceBus.Gateway.Tests.Channels
{
    using Gateway.Channels;
    using NUnit.Framework;

    [TestFixture]
    public class When_using_other_channel_types
    {
        private readonly string newChannelType = "NewKindOfChannel";

        [SetUp]
        public void SetUp()
        {
            ChannelTypes.RegisterChannelType("xyzzy", newChannelType);
        }

        [Test]
        public void Url_should_resolve_to_a_channel_type()
        {
            const string expectedChannelType = "Http";
            Assert.AreEqual(expectedChannelType, ChannelTypes.LookupByScheme("http"));
            Assert.AreEqual(expectedChannelType, ChannelTypes.LookupByScheme("https"));
            Assert.AreEqual(expectedChannelType, ChannelTypes.LookupByUrl("http://somewhere.out.there/"));
            Assert.AreEqual(expectedChannelType, ChannelTypes.LookupByUrl("https://somewhere.more.secure"));
            Assert.AreNotEqual(expectedChannelType, ChannelTypes.LookupByScheme("tcp"));
            Assert.AreEqual(newChannelType, ChannelTypes.LookupByScheme("xyzzy"));
            Assert.AreEqual("tcp", ChannelTypes.LookupByUrl("tcp://some.host"));
        }

    }
}
