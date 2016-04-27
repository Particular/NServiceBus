namespace NServiceBus.Core.Tests.Installers
{
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class PerformanceMonitorUsersInstallerTests
    {
        [Test]
        [Explicit]
        public Task Integration()
        {
            var installer = new PerformanceMonitorUsersInstaller();
            return installer.Install(@"location\username");
        }
    }
}
