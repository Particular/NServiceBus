namespace NServiceBus.Gateway.Tests.Routing
{
    using System.Configuration;
    using System.Linq;
    using Channels;
    using Config;
    using Gateway.Routing.Sites;
    using NUnit.Framework;

    [TestFixture]
    public class When_routing_using_the_configuration_source
    {

        [Test]
        public void Should_read_sites_and_their_keys_from_the_configSource()
        {
          
            var section = ConfigurationManager.GetSection(typeof(GatewayConfig).Name) as GatewayConfig;
          
            var router = new ConfigurationBasedSiteRouter
            {
                Sites = section.SitesAsDictionary()
            };

     
            var message = new TransportMessage();

            message.Headers.Add(Headers.DestinationSites, "SiteA");

            var sites = router.GetDestinationSitesFor(message);

            Assert.AreEqual(new Channel{ Address = "http://sitea.com",Type = "http"},sites.First().Channel);
        }
    }
}