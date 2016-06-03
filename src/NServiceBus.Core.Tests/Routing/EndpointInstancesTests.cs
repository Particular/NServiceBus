namespace NServiceBus.Core.Tests.Routing
{
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
