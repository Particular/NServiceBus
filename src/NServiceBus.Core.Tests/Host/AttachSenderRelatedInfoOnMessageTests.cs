namespace NServiceBus.Core.Tests.Host
{
    using System;
    using System.Collections.Generic;
    using Hosting;
    using NServiceBus.Pipeline;
    using NUnit.Framework;
    using Support;
    using Testing;

    [TestFixture]
    public class AddHostInfoHeadersBehaviorTests
    {
        [Test]
        public void Should_set_hosting_related_headers()
        {
            var endpoint = new EndpointInfo("MyEndpoint", false, null, "1.0.0");
            var hostInformation = new HostInformation(Guid.NewGuid(), "some display name");

            var context = InvokeBehavior(endpoint, hostInformation);

            Assert.AreEqual(endpoint.Name, context.Headers[Headers.OriginatingEndpoint]);
            Assert.AreEqual(endpoint.NServiceBusVersion, context.Headers[Headers.NServiceBusVersion]);
            Assert.AreEqual(hostInformation.HostId.ToString("N"), context.Headers[Headers.OriginatingHostId]);
            Assert.AreEqual(RuntimeEnvironment.MachineName, context.Headers[Headers.OriginatingMachine]);
        }

        [Test]
        public void Should_not_override_nsb_version_header()
        {
            var hostInformation = new HostInformation(Guid.NewGuid(), "some display name");
            var existingNServiceBusVersion = "some-crazy-version-number";
            var context = InvokeBehavior(new EndpointInfo("MyEndpoint", false, null, "1.0.0"),
                hostInformation,
                new Dictionary<string, string>
                {
                    {Headers.NServiceBusVersion, existingNServiceBusVersion}
                });

            Assert.AreEqual(existingNServiceBusVersion, context.Headers[Headers.NServiceBusVersion]);
        }

        static IOutgoingLogicalMessageContext InvokeBehavior(EndpointInfo endpoint, HostInformation hostInformation, Dictionary<string, string> headers = null)
        {
            var context = new TestableOutgoingLogicalMessageContext
            {
                Headers = headers ?? new Dictionary<string, string>()
            };

            new AddHostInfoHeadersBehavior(hostInformation, endpoint)
                .Invoke(context, _ => TaskEx.CompletedTask);

            return context;
        }
    }
}