namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstancesTests
    {
        [Test]
        public void Should_add_instances_grouped_by_endpoint_name()
        {
            var instances = new EndpointInstances();
            const string endpointName1 = "EndpointA";
            const string endpointName2 = "EndpointB";
            instances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(endpointName1),
                new EndpointInstance(endpointName2)
            });

            var salesInstances = instances.FindInstances(endpointName1);
            Assert.AreEqual(1, salesInstances.Count());

            var otherInstances = instances.FindInstances(endpointName2);
            Assert.AreEqual(1, otherInstances.Count());
        }

        [Test]
        public void Should_return_instances_configured_by_static_route()
        {
            var instances = new EndpointInstances();
            var sales = "Sales";
            instances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "1"),
                new EndpointInstance(sales, "2")
            });

            var salesInstances = instances.FindInstances(sales);
            Assert.AreEqual(2, salesInstances.Count());
        }

        [Test]
        public void Should_filter_out_duplicate_instances()
        {
            var instances = new EndpointInstances();
            var sales = "Sales";
            instances.AddOrReplaceInstances("A", new List<EndpointInstance>
            {
                new EndpointInstance(sales, "dup"),
                new EndpointInstance(sales, "dup")
            });

            var salesInstances = instances.FindInstances(sales);
            Assert.AreEqual(1, salesInstances.Count());
        }

        [Test]
        public void Should_default_to_single_instance_when_not_configured()
        {
            var instances = new EndpointInstances();
            var salesInstances = instances.FindInstances("Sales");

            var singleInstance = salesInstances.Single();
            Assert.IsNull(singleInstance.Discriminator);
            Assert.IsEmpty(singleInstance.Properties);
        }
    }
}
