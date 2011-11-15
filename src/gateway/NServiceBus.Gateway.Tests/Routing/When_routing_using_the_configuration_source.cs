using NUnit.Framework;

namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Gateway.Channels;
    using Gateway.Routing;
    using Gateway.Routing.Sites;
    using Unicast.Transport;

    [TestFixture]
    public class When_routing_using_the_configuration_source
    {
        IRouteMessagesToSites router;

        [SetUp]
        public void SetUp()
        {
            Configure.With(new Assembly[] {});
            router = new ConfigurationBasedSiteRouter();    
        }

        [Test]
        public void Should_read_sites_and_their_keys_from_the_configsource()
        {
             
            var message = new TransportMessage { Headers = new Dictionary<string, string>() };

            message.Headers.Add(Headers.DestinationSites, "SiteA");

            var sites = router.GetDestinationSitesFor(message);

            Assert.AreEqual(new Channel{ Address = "http://sitea.com",Type = "http"},sites.First().Channel);
        }
    }
}