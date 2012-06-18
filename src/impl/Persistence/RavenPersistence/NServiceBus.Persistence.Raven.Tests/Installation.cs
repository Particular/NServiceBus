namespace NServiceBus.Persistence.Raven.Tests
{
    using System;
    using System.IO;
    using System.Security.Principal;
    using System.Threading;
    using Config;
    using Config.ConfigurationSource;
    using Installation;
    using NServiceBus.Installation;
    using NUnit.Framework;

    [TestFixture]
    public class When_the_default_raven_persistence_is_used
    {

        [Test]
        public void Should_enable_raven_install()
        {
            ConfigureRavenPersistence.AutoCreateDatabase = false; 
            Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName(() => "Test")
                .DefaultBuilder()
                .RavenPersistence();

            Assert.True(RavenDBInstaller.InstallEnabled);
        }

     
    }

    [TestFixture]
    public class When_the_endpoint_is_not_running_on_the_masternode
    {
        
        [Test]
        public void Should_disable_raven_install()
        {
            TestMasterNodeOverride.FakeMasterNode = ()=> "some_other_server";
            ConfigureRavenPersistence.AutoCreateDatabase = false;
            Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName(() => "Test")
                .DefaultBuilder()
                .RavenPersistence();
            TestMasterNodeOverride.FakeMasterNode = ()=>"";
 
            Assert.False(RavenDBInstaller.InstallEnabled);
        }


    }



    public class When_a_custom_connection_string_is_used
    {

        [Test]
        public void Should_disable_raven_install()
        {
            Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName(() => "Test")
                .DefaultBuilder()
                .RavenPersistence("Raven");

            Assert.False(RavenDBInstaller.InstallEnabled);
        }


    }
    public class TestMasterNodeOverride : IProvideConfiguration<MasterNodeConfig>
    {
        public static Func<string> FakeMasterNode = ()=>"";

        public MasterNodeConfig GetConfiguration()
        {
            return new MasterNodeConfig
                       {
                           Node = FakeMasterNode()
                       };
        }
    }


    [TestFixture]
    public class When_the_infrastructure_installers_run
    {
        [Test, Explicit("")]
        public void It_should_unpack_and_install_ravendb()
        {
            if (Directory.Exists(RavenDBInstaller.RavenInstallPath))
                Directory.Delete(RavenDBInstaller.RavenInstallPath, true);



            Configure.With()
                .DefineEndpointName(() => "Test")
                .DefaultBuilder()
                .RavenPersistence();

            var installer = new RavenDBInstaller() as INeedToInstallInfrastructure;


            installer.Install(Thread.CurrentPrincipal.Identity as WindowsIdentity);
        }

     
    }
}