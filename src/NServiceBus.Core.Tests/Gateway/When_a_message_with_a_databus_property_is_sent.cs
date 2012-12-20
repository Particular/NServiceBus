namespace NServiceBus.Gateway.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NUnit.Framework;

    [TestFixture, Ignore("Need to redo all this tests because the gateway is now a satellite!")]
    public class When_a_message_with_a_databus_property_is_sent : via_the_gateway
    {
        [Test]
        public void Should_transmit_the_databus_payload_on_the_same_channel_as_the_message()
        {
            var headers = new Dictionary<string, string>();
            var dataBusKey = "NServiceBus.DataBus." + Guid.NewGuid();


            var timeToLive = TimeSpan.FromDays(1);
            var expectedExpiryTime = DateTime.Now + timeToLive;

            using (var stream = new MemoryStream(new byte[1]))
                headers[dataBusKey] = databusForSiteA.Put(stream, timeToLive);

            SendMessage(HttpAddressForSiteB,headers);

           
            var transportMessage = GetDetailsForReceivedMessage().Message;

            string databusKeyForSiteB;

            transportMessage.Headers.TryGetValue(dataBusKey, out databusKeyForSiteB);

            //make sure that we got the key
            Assert.NotNull(databusKeyForSiteB);

            //make sure that they key exist in the DataBus for SiteB
            Assert.NotNull(databusForSiteB.Get(databusKeyForSiteB));

            //make sure that the time to live was transmitted properly
            Assert.GreaterOrEqual(databusForSiteB.Peek(databusKeyForSiteB).ExpireAt,expectedExpiryTime);
 
        }
    }
}