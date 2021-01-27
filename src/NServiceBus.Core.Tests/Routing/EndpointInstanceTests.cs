namespace NServiceBus.Core.Tests.Routing
{
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class EndpointInstanceTests
    {
        [Test]
        public void Two_instances_with_same_properties_are_equal()
        {
            var i1 = new EndpointInstance("Endpoint").SetProperty("P1", "V1").SetProperty("P2", "V2");
            var i2 = new EndpointInstance("Endpoint").SetProperty("P2", "V2").SetProperty("P1", "V1");

            Assert.IsTrue(i1.Equals(i2));
        }
    }
}