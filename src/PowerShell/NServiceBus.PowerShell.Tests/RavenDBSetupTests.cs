namespace NServiceBus.PowerShell.Tests
{
    using System.IO;
    using NUnit.Framework;
    using Setup.Windows.RavenDB;

    [TestFixture]
    public class RavenDBSetupTests
    {
        [Explicit]
        [Test]
        public void Install()
        {
            RavenDBSetup.Install();
        }

        [Test]
        public void EnsureGetRavenResourcesIsNotEmpty()
        {
            var combine = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                Directory.CreateDirectory(combine);
                RavenDBSetup.ExportRavenResources(combine);
                Assert.IsNotEmpty(Directory.GetFiles(combine));
            }
            finally
            {
                Directory.Delete(combine, true);
            }
        }
    }
}
