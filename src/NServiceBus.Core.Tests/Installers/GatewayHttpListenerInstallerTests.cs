namespace NServiceBus.Core.Tests.Installers
{
    using System.Diagnostics;
    using System.Security.Principal;
    using Installation;
    using NUnit.Framework;

    [TestFixture]
    public class GatewayHttpListenerInstallerTests
    {
        [Test]
        [Explicit]
        public void Integration()
        {
            //Needs to be run as elevated
            GatewayHttpListenerInstaller.StartNetshProcess(WindowsIdentity.GetCurrent().Name, 4567);

            //To list existing acls
            //netsh http show urlacl
        }

        [Test]
        [Explicit]
        public void Delete()
        {
            var args = string.Format(@"http delete urlacl url=http://+:{0}/ ", 4567);

            var process = Process.Start(new ProcessStartInfo
                                        {
                                            Verb = "runas",
                                            Arguments = args,
                                            FileName = "netsh",
                                        });
            process.WaitForExit();
        }
    }
}