namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstancesTests
    {
        [Test]
        public void Should_throw_when_trying_to_configure_instances_that_do_not_match_endpoint_name()
        {
            var instances = new EndpointInstances();
            TestDelegate action = () => instances.Add(new EndpointInstance("Sales"), new EndpointInstance("A"));
            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public async Task Should_return_instances_configured_by_static_route()
        {
            var instances = new EndpointInstances();
            var sales = "Sales";
            instances.Add(new EndpointInstance(sales, "1"), new EndpointInstance(sales, "2"));

            var salesInstances = await instances.FindInstances(sales);
            Assert.AreEqual(2, salesInstances.Count());
        }

        [Test]
        public async Task Should_filter_out_duplicate_instances()
        {
            var instances = new EndpointInstances();
            var sales = "Sales";
            instances.Add(new EndpointInstance(sales, "dup"), new EndpointInstance(sales, "dup"));

            var salesInstances = await instances.FindInstances(sales);
            Assert.AreEqual(1, salesInstances.Count());
        }

        [Test]
        public async Task Should_default_to_single_instance_when_not_configured()
        {
            var instances = new EndpointInstances();
            var salesInstancess = await instances.FindInstances("Sales");

            var singleInstance = salesInstancess.Single();
            Assert.IsNull(singleInstance.Discriminator);
            Assert.IsEmpty(singleInstance.Properties);
        }
    }
}