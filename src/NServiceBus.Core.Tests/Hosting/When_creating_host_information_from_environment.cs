namespace NServiceBus.Hosting.Tests
{
    using System;
    using Hosting;
    using NUnit.Framework;

    [TestFixture]
    public class When_creating_host_information_from_environment
    {
        [Test]
        public void HostId_is_parsed_from_path_without_spaces_but_with_quotes()
        {
            var information1 = HostInformation.CreateHostInformation("\"pathto\\mysuperduper.exe\" somevar", "MyMachine");

            Assert.IsTrue(information1.HostId == Guid.Parse("{8dd7bbcc-dfc3-d84a-41ac-82c14f1ed7db}"));
        }

        [Test]
        public void HostId_is_parsed_from_path_without_spaces_but_without_quotes()
        {
            var information2 = HostInformation.CreateHostInformation("pathto\\mysuperduper.exe somevar", "MyMachine");

            Assert.IsTrue(information2.HostId == Guid.Parse("{8dd7bbcc-dfc3-d84a-41ac-82c14f1ed7db}"));
        }

        [Test]
        public void Both_hostIds_for_paths_without_spaces_are_equal()
        {
            var information1 = HostInformation.CreateHostInformation("\"pathto\\mysuperduper.exe\" somevar", "MyMachine");
            var information2 = HostInformation.CreateHostInformation("pathto\\mysuperduper.exe somevar", "MyMachine");

            Assert.IsTrue(information1.HostId == information2.HostId);
        }

        [Test]
        public void HostId_is_parsed_from_path_with_spaces_having_a_parameter_with_spaces()
        {
            var information3 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"", "MyMachine");

            Assert.IsTrue(information3.HostId == Guid.Parse("{db3ff7ff-508f-cce7-0a8b-0092a7d750b9}"));
        }

        [Test]
        public void HostId_is_parsed_from_path_with_spaces_having_a_parameter_without_spaces()
        {
            var information4 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" somevar", "MyMachine");

            Assert.IsTrue(information4.HostId == Guid.Parse("{db3ff7ff-508f-cce7-0a8b-0092a7d750b9}"));
        }

        [Test]
        public void Both_hostIds_for_paths_with_spaces_are_equal()
        {
            var information3 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" \"somevar with spaces\"", "MyMachine");
            var information4 = HostInformation.CreateHostInformation("\"pathto\\mysuper duper.exe\" somevar", "MyMachine");

            Assert.IsTrue(information3.HostId == information4.HostId);
        }

    }
}
