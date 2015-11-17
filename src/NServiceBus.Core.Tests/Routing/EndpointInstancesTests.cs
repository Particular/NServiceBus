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
        public void Should_throw_when_trying_to_configure_instances_that_don_not_match_endpoint_name()
        {
            var instances = new EndpointInstances();
            TestDelegate action = () => instances.AddStatic(new EndpointName("Sales"), new EndpointInstanceName(new EndpointName("A"), null, null));
            Assert.Throws<ArgumentException>(action);
        }

        [Test]
        public void Should_return_instances_configured_by_static_route()
        {
            var instances = new EndpointInstances();
            var sales = new EndpointName("Sales");
            instances.AddStatic(sales, new EndpointInstanceName(sales, "1", null), new EndpointInstanceName(sales, "2", null));

            var salesInstances = instances.FindInstances(sales).ToList();
            Assert.AreEqual(2, salesInstances.Count);
        }

        [Test]
        public void Should_filter_out_duplicate_instances()
        {
            var instances = new EndpointInstances();
            var sales = new EndpointName("Sales");
            instances.AddStatic(sales, new EndpointInstanceName(sales, "dup", null), new EndpointInstanceName(sales, "dup", null));

            var salesInstances = instances.FindInstances(sales).ToList();
            Assert.AreEqual(1, salesInstances.Count);
        }

        [Test]
        public void Should_throw_when_trying_to_enumerate_collection_of_instances_of_unknown_endpoint()
        {
            var instances = new EndpointInstances();
            var salesInstances = instances.FindInstances(new EndpointName("Sales"));
            TestDelegate action = () => salesInstances.ToArray();
            Assert.Throws<Exception>(action);
        }
    }
}