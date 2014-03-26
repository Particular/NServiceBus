namespace NServiceBus.MessageInterfaces.Tests
{
    using System;
    using Hosting;
    using NUnit.Framework;

    [TestFixture]
    public class When_creating_host_information_from_environment
    {
        HostInformation information1;
        HostInformation information2;
        HostInformation information3;
        HostInformation information4;

        [SetUp]
        public void SetUp()
        {
            information1 = HostInformation.CreateHostInformation("\"pathto\\mysuperduper.exe\" somevar", "MyMachine");
            information2 = HostInformation.CreateHostInformation("pathto\\mysuperduper.exe somevar", "MyMachine");
            information3 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"", "MyMachine");
            information4 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" somevar", "MyMachine");
        }

        [Test]
        public void HostId_is_parsed_from_path_without_spaces_but_with_quotes()
        {
            Assert.IsTrue(information1.HostId != default(Guid));
        }

        [Test]
        public void HostId_is_parsed_from_path_without_spaces_but_without_quotes()
        {
            Assert.IsTrue(information2.HostId != default(Guid));
        }

        [Test]
        public void Both_hostIds_for_paths_without_spaces_are_equal()
        {
            Assert.IsTrue(information1.HostId == information2.HostId);
        }

        [Test]
        public void HostId_is_parsed_from_path_with_spaces_having_a_parameter_with_spaces()
        {
            Assert.IsTrue(information3.HostId != default(Guid));
        }

        [Test]
        public void HostId_is_parsed_from_path_with_spaces_having_a_parameter_without_spaces()
        {
            Assert.IsTrue(information4.HostId != default(Guid));
        }

        [Test]
        public void Both_hostIds_for_paths_with_spaces_are_equal()
        {
            Assert.IsTrue(information3.HostId == information4.HostId);
        }

    }
}
