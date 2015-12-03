namespace NServiceBus.Core.Tests.Routing
{
    using System;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NUnit.Framework;

    [TestFixture]
    public class TransportAddressesTest
    {
        [Test]
        public void Special_cases_should_override_rules()
        {
            var addresses = new TransportAddresses(address => null);
            addresses.AddRule(i => "Rule");
            addresses.AddSpecialCase(new EndpointInstance(new EndpointName("Sales"), null, null), "SpecialCase");

            Assert.AreEqual("SpecialCase", addresses.GetTransportAddress(new LogicalAddress(new EndpointInstance("Sales"))));
            Assert.AreEqual("Rule", addresses.GetTransportAddress(new LogicalAddress(new EndpointInstance("Billing"))));
        }

        [Test]
        public void Rules_should_override_transport_defaults()
        {
            var addresses = new TransportAddresses(address => "TransportDefault");
            addresses.AddRule(i => i.EndpointInstance.Endpoint.ToString().StartsWith("S") ? "Rule" : null);
            

            Assert.AreEqual("Rule", addresses.GetTransportAddress(new LogicalAddress(new EndpointInstance("Sales"))));
            Assert.AreEqual("TransportDefault", addresses.GetTransportAddress(new LogicalAddress(new EndpointInstance("Billing"))));
        }

        [Test]
        public void It_should_throw_when_rules_are_ambiguous()
        {
            var addresses = new TransportAddresses(address => null);
            addresses.AddRule(i => i.EndpointInstance.Endpoint.ToString().StartsWith("S") ? "Rule1" : null);
            addresses.AddRule(i => i.EndpointInstance.Endpoint.ToString().EndsWith("s") ? "Rule2" : null);

            TestDelegate action = () => addresses.GetTransportAddress(new LogicalAddress(new EndpointInstance("Sales")));
            Assert.Throws<Exception>(action);
        }
    }
}