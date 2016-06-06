namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstancesTests
    {
        [Test]
        public async Task Should_add_instances_grouped_by_endpoint_name()
        {
            var instances = new EndpointInstances();
            var endpointName1 = "EndpointA";
            var endpointName2 = "EndpointB";
            instances.Add(new EndpointInstance(endpointName1), new EndpointInstance(endpointName2));

            var salesInstances = await instances.FindInstances(endpointName1);
            Assert.AreEqual(1, salesInstances.Count());

            var otherInstances = await instances.FindInstances(endpointName2);
            Assert.AreEqual(1, otherInstances.Count());
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
            instances.AddDynamic(e => Task.FromResult(new List<EndpointInstance> { new EndpointInstance(sales, "dup") }.AsEnumerable()));

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

        [Test]
        public async Task Should_evaluate_dynamic_rules_on_each_call()
        {
            var instances = new EndpointInstances();
            var endpointName = "endpointA";
            instances.Add(new EndpointInstance(endpointName, "1"));
            var invocationCounter = 0;
            instances.AddDynamic(e => Task.FromResult(e == endpointName && invocationCounter++ == 0 ? new [] { new EndpointInstance(endpointName, "2") }.AsEnumerable() : Enumerable.Empty<EndpointInstance>()));

            var result1 = await instances.FindInstances(endpointName);
            var result2 = await instances.FindInstances(endpointName);

            Assert.AreEqual(2, result1.Count());
            Assert.AreEqual(1, result2.Count());
        }
    }
}
