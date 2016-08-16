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
                new EndpointInstance(endpointA, "1"),
                new EndpointInstance(endpointA, "2")
            };

            var endpointB = "endpointB";
            var endpointBInstances = new[]
            {
                new EndpointInstance(endpointB, "1"),
                new EndpointInstance(endpointB, "2")
            };

            var result = new List<EndpointInstance>();
            result.Add(InvokeDistributionStrategy(policy, endpointAInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointBInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointAInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointBInstances));

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[1]));
        }

        static EndpointInstance InvokeDistributionStrategy(IDistributionPolicy policy, EndpointInstance[] instances)
        {
            return policy.GetDistributionStrategy(instances[0].Endpoint).SelectReceiver(instances);
        }
    }
}