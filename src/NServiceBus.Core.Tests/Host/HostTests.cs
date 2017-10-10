namespace NServiceBus.Core.Tests.Host
{
    using System;
    using NUnit.Framework;
    using Host = NServiceBus.Host;

    [TestFixture]
    public class HostTests
    {
        [Test]
        public void When_not_running_ASP_NET_should_choose_BaseDirectory_as_output_directory()
        {
            var directory = Host.GetOutputDirectory();

            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory, directory);
        }
    }
}