namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstanceTests
    {
        [Test]
        public void Two_instances_with_same_addresses_are_equal()
        {
            var i1 = new EndpointInstance("Endpoint", "InstanceX");
            var i2 = new EndpointInstance("Endpoint", "InstanceX");

            Assert.IsTrue(i1.Equals(i2));
        }
    }
}