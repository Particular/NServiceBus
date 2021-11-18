namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System;
    using NServiceBus.Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    public class When_transport_is_not_configured : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_if_receive_addresses_are_resolved()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

#pragma warning disable IDE0079
#pragma warning disable CS0618
            var localAddressEx = Assert.Throws<InvalidOperationException>(() => endpointConfiguration.GetSettings().LocalAddress(), "Should throw since the transport isn't configured yet");
            StringAssert.Contains("LocalAddress isn't available until the transport is configured", localAddressEx.Message);

            var instanceAddressEx = Assert.Throws<InvalidOperationException>(() => endpointConfiguration.GetSettings().InstanceSpecificQueue(), "Should throw since the transport isn't configured yet");
            StringAssert.Contains("Instance-specific receive address isn't available until the transport is configured", instanceAddressEx.Message);
#pragma warning restore CS0618
#pragma warning restore IDE0079
        }
    }
}