namespace NServiceBus.Core.Tests.Installers
{
    using Installation;
    using NUnit.Framework;

    [TestFixture]
    public class PerformanceMonitorUsersInstallerTests
    {
        [Test]
        [Explicit]
        public void Integration()
        {
            var installer = new PerformanceMonitorUsersInstaller();
            installer.Install(@"location\username");
        }
    }
}
