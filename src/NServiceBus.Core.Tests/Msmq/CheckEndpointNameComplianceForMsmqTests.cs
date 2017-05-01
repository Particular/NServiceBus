namespace NServiceBus.Core.Tests.Msmq
{
    using System;
    using NServiceBus.Transports.Msmq;
    using NUnit.Framework;

    [TestFixture]
    public class CheckEndpointNameComplianceForMsmqTests
    {
        [Test]
        public void Should_throw_if_endpoint_name_is_too_long()
        {
            const string endpointName = "ThisisaloooooooooooooooooooooooooooooooooooooooooooooooooooongQueeeeeeeeeeeeeeeeeeeeeeeeeeee1234";
            var checker = new CheckEndpointNameComplianceForMsmq();
            var ex = Assert.Throws<InvalidOperationException>(() => checker.Check(endpointName));
            StringAssert.Contains(endpointName, ex.Message);
        }

        [Test]
        public void Should_not_throw_if_endpoint_name_is_compliant()
        {
            const string endpointName = "ThisisaloooooooooooooooooooooooooooooooooooooooooooooooooooongQueeeeeeeeeeeeeeeeeeeeeeeeeeee123";
            var checker = new CheckEndpointNameComplianceForMsmq();
            Assert.DoesNotThrow(() =>checker.Check(endpointName));
        }
    }
}
