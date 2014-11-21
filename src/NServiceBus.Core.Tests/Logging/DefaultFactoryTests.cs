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
        public void When_not_running_in_http_DeriveAppDataPath_should_throw()
        {
            var exception = Assert.Throws<Exception>(() => DefaultFactory.DeriveAppDataPath());
            Assert.AreEqual("Detected running in a website and attempted to use HostingEnvironment.MapPath(\"~/App_Data/\") to derive the logging path. Failed since MapPath returned null. To avoid using HostingEnvironment.MapPath to derive the logging directory you can instead configure it to a specific path using LogManager.Use<DefaultFactory>().Directory(\"pathToLoggingDirectory\");", exception.Message);
        }
    }


}
