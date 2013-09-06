namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Linq;
    using System.Reflection;
    using Channels;
    using Gateway.Routing;
    using Gateway.Routing.Sites;
    using NUnit.Framework;

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
             
            var message = new TransportMessage();

            message.Headers.Add(Headers.DestinationSites, "SiteA");

            var sites = router.GetDestinationSitesFor(message);

            Assert.AreEqual(new Channel{ Address = "http://sitea.com",Type = "http"},sites.First().Channel);
        }
    }
}