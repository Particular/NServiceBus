namespace NServiceBus.Core.Tests.Installers
{
    using System.Threading.Tasks;
    using NServiceBus.Performance.Counters;
    using NUnit.Framework;

    [TestFixture]
    public class PerformanceMonitorUsersInstallerTests
    {
        [Test]
        [Explicit]
        public async Task Integration()
        {
            var installer = new PerformanceMonitorUsersInstaller();
            await installer.InstallAsync(@"location\username", null);
        }
    }
}
