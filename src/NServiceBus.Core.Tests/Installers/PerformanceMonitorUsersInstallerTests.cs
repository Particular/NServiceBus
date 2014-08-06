namespace NServiceBus.Core.Tests.Installers
{
    using NUnit.Framework;

    [TestFixture]
    public class PerformanceMonitorUsersInstallerTests
    {
        [Test]
        [Explicit]
        public void Integration()
        {
            PerformanceMonitorUsersInstaller.Install(@"location\username");
        }
    }
}
