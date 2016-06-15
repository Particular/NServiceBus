namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class RoutingPolicyTests
    {
        [Test]
        public void ShouldScopeDistributionToEndpointName()
        {
            var endpointA = "endpointA";
            var policy = new DistributionPolicy();
            var endpointAInstances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointA, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointA, "2")),
            };

            var endpointB = "endpointB";
            var endpointBInstances = new[]
            {
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointB, "1")),
                UnicastRoutingTarget.ToEndpointInstance(new EndpointInstance(endpointB, "2")),
            };

            var result = new List<UnicastRoutingTarget>();
            result.AddRange(InvokeDistributionStrategy(policy, endpointAInstances));
            result.AddRange(InvokeDistributionStrategy(policy, endpointBInstances));
            result.AddRange(InvokeDistributionStrategy(policy, endpointAInstances));
            result.AddRange(InvokeDistributionStrategy(policy, endpointBInstances));

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[1]));
        }

        static IEnumerable<UnicastRoutingTarget> InvokeDistributionStrategy(DistributionPolicy policy, UnicastRoutingTarget[] instances)
        {
            return policy.GetDistributionStrategy(instances[0].Endpoint).SelectDestination(instances);
        }
    }
}