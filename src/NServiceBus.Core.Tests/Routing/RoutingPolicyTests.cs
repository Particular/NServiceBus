namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using NUnit.Framework;
    using Testing;

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
                endpointA + "1",
                endpointA + "2"
            };

            var endpointB = "endpointB";
            var endpointBInstances = new[]
            {
                endpointB + "1",
                endpointB + "2"
            };

            var result = new List<string>();
            result.Add(InvokeDistributionStrategy(policy, endpointA, endpointAInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointB, endpointBInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointA, endpointAInstances));
            result.Add(InvokeDistributionStrategy(policy, endpointB, endpointBInstances));

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[1]));
        }

        static string InvokeDistributionStrategy(IDistributionPolicy policy, string endpointName, string[] instanceAddress)
        {
            return policy.GetDistributionStrategy(endpointName, DistributionStrategyScope.Send).SelectDestination(instanceAddress, new TestableOutgoingContext());
        }
    }
}