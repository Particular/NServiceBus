namespace NServiceBus.Core.Tests.Logging
{
    using System;
    using System.IO;
    using NServiceBus.Logging;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultFactoryTests
    {
        [Test]
        public void When_directory_is_bad_should_throw()
        {
            var defaultFactory = new DefaultFactory();
            var nonExistingDirectoryException = Assert.Throws<DirectoryNotFoundException>(() => defaultFactory.Directory("baddir"));
            Assert.AreEqual("Could not find logging directory: 'baddir'", nonExistingDirectoryException.Message);
            Assert.Throws<ArgumentNullException>(() => defaultFactory.Directory(null));
            Assert.Throws<ArgumentNullException>(() => defaultFactory.Directory(""));
            Assert.Throws<ArgumentNullException>(() => defaultFactory.Directory(" "));
        }

        [Test]
        public void When_not_running_ASP_NET_should_choose_BaseDirectory_as_logging_directory()
        {
            var directory = Host.GetOutputDirectory();

            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory, directory);
        }
    }


}
