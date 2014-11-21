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
        public void When_not_running_in_http_should_return_BaseDirectory()
        {
            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory, DefaultFactory.FindDefaultLoggingDirectory());
        }
        
    }


}
