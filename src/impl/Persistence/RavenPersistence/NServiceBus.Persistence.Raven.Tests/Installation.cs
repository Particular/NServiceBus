namespace NServiceBus.Persistence.Raven.Tests
{
    using System.IO;
    using System.Security.Principal;
    using System.Threading;
    using Installation;
    using NUnit.Framework;

    [TestFixture]
    public class When_installers_are_run
    {
     
        [Test,Explicit("")]
        public void It_should_unpack_and_install_ravendb_if_needed()
        {
            if (Directory.Exists(Configure.Instance.RavenInstallPath()))
                Directory.Delete(Configure.Instance.RavenInstallPath(),true);



            Configure.With()
                .DefineEndpointName(()=>"Test")
                .DefaultBuilder()
                .RavenPersistence();

            var installer = new RavenDBInstaller();


            installer.Install(Thread.CurrentPrincipal.Identity as WindowsIdentity);
        }

     
    }
}