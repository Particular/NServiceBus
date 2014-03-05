namespace NServiceBus.Core.Tests
{
    using System.Collections.Generic;
    using Gateway.Channels.Http;
    using Gateway.HeaderManagement;
    using Gateway.Receiving;
    using NUnit.Framework;

    [TestFixture]
    public class ChannelReceiverHeaderReaderTests
    {

        [Test]
        public void Missing_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadCallType(new Dictionary<string, string>()));
        }

        [Test]
        public void EmptyString_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadCallType(new Dictionary<string, string>
                        {
                            {GatewayHeaders.CallTypeHeader, ""},
                        }));
        }

        [Test]
        public void Invalid_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadCallType(new Dictionary<string, string>
                        {
                            {GatewayHeaders.CallTypeHeader, "badValue"},
                        }));
        }

        [Test]
        public void EmptyString_ClientId_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadClientId(new Dictionary<string, string>
                        {
                            {GatewayHeaders.ClientIdHeader, ""},
                        }));
        }

        [Test]
        public void Missing_ClientId_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadClientId(new Dictionary<string, string>()));
        }

        [Test]
        public void Missing_MD5_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadMd5(new Dictionary<string, string>()));
        }

        [Test]
        public void Empty_MD5_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => ChannelReceiverHeaderReader.ReadMd5( new Dictionary<string, string>
                        {
                            {HttpHeaders.ContentMd5Key, ""},
                        }));
        }
    }
}