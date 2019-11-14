namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using NUnit.Framework;
    using System.Reflection;

    [TestFixture]
    public class When_configuring_assembly_scanning
    {
        [Test]
        public void Should_provide_reflection_backdoor_via_endpoint_config()
        {
            // This is needed for
            // * docs - https://github.com/Particular/docs.particular.net/blob/master/Snippets/Core/Core_7/Headers/Writers/EndpointConfigurationExtensions.cs
            // * metrics tests - https://github.com/Particular/NServiceBus.Metrics.PerformanceCounters/blob/master/src/NServiceBus.Metrics.PerformanceCounters.Tests/EndpointConfigurationExtensions.cs
            // * we also have users relying on this to prevent assembly scanning from happening
            Assert.NotNull(typeof(EndpointConfiguration).GetMethod("TypesToScanInternal", BindingFlags.NonPublic | BindingFlags.Instance));
        }
    }
}