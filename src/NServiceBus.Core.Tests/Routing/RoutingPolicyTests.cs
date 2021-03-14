namespace NServiceBus.Core.Tests.Routing
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.Routing;
    using NUnit.Framework;

    [TestFixture]
    public class RoutingPolicyTests
    {
        [Test]
        public async Task ShouldScopeDistributionToEndpointName()
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

            var result = new List<string>
            {
                await InvokeDistributionStrategy(policy, endpointA, endpointAInstances),
                await InvokeDistributionStrategy(policy, endpointB, endpointBInstances),
                await InvokeDistributionStrategy(policy, endpointA, endpointAInstances),
                await InvokeDistributionStrategy(policy, endpointB, endpointBInstances)
            };

            Assert.That(result.Count, Is.EqualTo(4));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointAInstances[1]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[0]));
            Assert.That(result, Has.Exactly(1).EqualTo(endpointBInstances[1]));
        }

        static Task<string> InvokeDistributionStrategy(IDistributionPolicy policy, string endpointName, string[] instanceAddress, CancellationToken cancellationToken = default)
        {
            return policy.GetDistributionStrategy(endpointName, DistributionStrategyScope.Send).SelectDestination(new DistributionContext(instanceAddress, null, null, null, null, null), cancellationToken);
        }
    }
}