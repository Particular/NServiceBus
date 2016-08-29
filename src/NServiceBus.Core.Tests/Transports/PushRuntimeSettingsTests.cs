namespace NServiceBus.Core.Tests.Transports
{
    using System;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class PushRuntimeSettingsTests
    {
        [Test]
        public void Should_default_concurrency_to_num_processors()
        {
            Assert.AreEqual(Math.Max(2, Environment.ProcessorCount), new PushRuntimeSettings().MaxConcurrency);
        }

        [Test]
        public void Should_honor_explicit_concurrency_settings()
        {
            Assert.AreEqual(10, new PushRuntimeSettings(10).MaxConcurrency);
        }
    }
}