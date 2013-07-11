namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using Channels;
    using Gateway.Routing.Sites;
    using NUnit.Framework;

    [TestFixture]
    public class When_routing_a_reply_message
    {
        [Test]
        public void Should_return_the_correct_site_based_on_the_originating_site_header()
        {
            var router = new OriginatingSiteHeaderRouter();

            var message = new TransportMessage();

            var defaultChannel = new Channel
                                     {
                                         Type = "http",
                                         Address = "http://x.y"

                                     };

            message.Headers.Add(Headers.OriginatingSite, defaultChannel.ToString());

            Assert.AreEqual(defaultChannel, router.GetDestinationSitesFor(message).First().Channel);
        }


        [Test]
        public void Should_return_empty_list_if_header_is_missing()
        {
            var router = new OriginatingSiteHeaderRouter();

            var message = new TransportMessage();
            
            Assert.AreEqual(0, router.GetDestinationSitesFor(message).Count());
        }
    }
}