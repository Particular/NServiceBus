namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstancesTests
    {
        [Test]
        public void Should_throw_when_trying_to_configure_instances_that_do_not_match_endpoint_name()
        {
            var instances = new EndpointInstances();
            TestDelegate action = () => instances.AddStatic(new EndpointName("Sales"), new EndpointInstance(new EndpointName("A")));
            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void Should_return_instances_configured_by_static_route()
        {
            var instances = new EndpointInstances();
            var sales = new EndpointName("Sales");
            instances.AddStatic(sales, new EndpointInstance(sales, "1", null), new EndpointInstance(sales, "2"));

            var salesInstances = instances.FindInstances(sales).ToList();
            Assert.AreEqual(2, salesInstances.Count);
        }

        [Test]
        public void Should_filter_out_duplicate_instances()
        {
            var instances = new EndpointInstances();
            var sales = new EndpointName("Sales");
            instances.AddStatic(sales, new EndpointInstance(sales, "dup", null), new EndpointInstance(sales, "dup"));

            var salesInstances = instances.FindInstances(sales).ToList();
            Assert.AreEqual(1, salesInstances.Count);
        }

        [Test]
        public void Should_default_to_single_instance_when_not_configured()
        {
            var instances = new EndpointInstances();
            var salesInstances = instances.FindInstances(new EndpointName("Sales")).ToArray();
            Assert.AreEqual(1, salesInstances.Length);
            Assert.IsNull(salesInstances[0].Discriminator);
            Assert.IsEmpty(salesInstances[0].Properties);
        }
    }
}