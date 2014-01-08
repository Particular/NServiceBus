namespace NServiceBus.Core.Tests
{
    using System.Collections.Generic;
    using Gateway.Channels;
    using Gateway.Channels.Http;
    using Gateway.HeaderManagement;
    using Gateway.Receiving;
    using Gateway.Utils;
    using NUnit.Framework;

    [TestFixture]
    public class IdempotentChannelReceiverTests
    {
        [Test]
        public void Missing_MD5_throws_ChannelException()
        {
            var data = new DataReceivedOnChannelArgs
                {
                    Headers = new Dictionary<string, string>()
                };
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.VerifyData(data));
        }

        [Test]
        public void Empty_MD5_throws_ChannelException()
        {
            var data = new DataReceivedOnChannelArgs
                {
                    Headers = new Dictionary<string, string>
                        {
                            {HttpHeaders.ContentMd5Key, ""},
                        }
                };
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.VerifyData(data));
        }

        [Test]
        public void Valid_Md5_can_be_verified()
        {
            var data = new DataReceivedOnChannelArgs
                {
                    Headers = new Dictionary<string, string>
                        {
                            {HttpHeaders.ContentMd5Key, Hasher.Hash("myData".ConvertToStream())},
                        },
                    Data = "myData".ConvertToStream()
                };
            IdempotentChannelReceiver.VerifyData(data);
        }

        [Test]
        public void Invalid_hash_throws_ChannelException()
        {
            var data = new DataReceivedOnChannelArgs
                {
                    Headers = new Dictionary<string, string>
                        {
                            {HttpHeaders.ContentMd5Key, "invalidHash"},
                        },
                    Data = "myData".ConvertToStream()
                };
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.VerifyData(data));
        }

        [Test]
        public void Missing_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.ReadCallType(new Dictionary<string, string>()));
        }

        [Test]
        public void EmptyString_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.ReadCallType(new Dictionary<string, string>
                        {
                            {GatewayHeaders.CallTypeHeader, ""},
                        }));
        }

        [Test]
        public void Invalid_CallTypeHeader_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.ReadCallType(new Dictionary<string, string>
                        {
                            {GatewayHeaders.CallTypeHeader, "badValue"},
                        }));
        }

        [Test]
        public void EmptyString_ClientId_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.ReadClientId(new Dictionary<string, string>
                        {
                            {GatewayHeaders.ClientIdHeader, ""},
                        }));
        }

        [Test]
        public void Missing_ClientId_throws_ChannelException()
        {
            Assert.Throws<ChannelException>(() => IdempotentChannelReceiver.ReadClientId(new Dictionary<string, string>()));
        }
    }
}