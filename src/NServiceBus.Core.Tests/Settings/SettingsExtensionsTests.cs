namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
    using System;
    using Configuration.AdvancedExtensibility;
    using NUnit.Framework;

    [TestFixture]
    public class SettingsExtensionsTests
    {
        [Test]
        public void Should_throw_if_receive_addresses_are_accessed_before_transport_configuration()
        {
            var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

#pragma warning disable IDE0079
#pragma warning disable CS0618
            var localAddressEx = Assert.Throws<InvalidOperationException>(() => endpointConfiguration.GetSettings().LocalAddress(), "Should throw since the endpoint hasn't been fully configured yet");
            StringAssert.Contains("LocalAddress isn't available until the endpoint configuration is complete.", localAddressEx.Message);

            var instanceAddressEx = Assert.Throws<InvalidOperationException>(() => endpointConfiguration.GetSettings().InstanceSpecificQueue(), "Should throw since the endpoint hasn't been fully configured yet");
            StringAssert.Contains("Instance-specific receive address isn't available until the endpoint configuration is complete.", instanceAddressEx.Message);
#pragma warning restore CS0618
#pragma warning restore IDE0079
        }
    }
}