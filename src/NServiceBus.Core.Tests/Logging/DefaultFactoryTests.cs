namespace NServiceBus.Core.Tests.Logging;

using System;
using System.IO;
using NServiceBus.Logging;
using NUnit.Framework;

#pragma warning disable CS0618 // Tests intentionally exercise deprecated DefaultFactory APIs
[TestFixture]
public class DefaultFactoryTests
{
    [Test]
    public void When_directory_is_bad_should_throw()
    {
        var defaultFactory = new DefaultFactory();
        var nonExistingDirectoryException = Assert.Throws<DirectoryNotFoundException>(() => defaultFactory.Directory("baddir"));
        Assert.That(nonExistingDirectoryException.Message, Is.EqualTo("Could not find logging directory: 'baddir'"));
        Assert.Throws<ArgumentNullException>(() => defaultFactory.Directory(null));
        Assert.Throws<ArgumentException>(() => defaultFactory.Directory(""));
        Assert.Throws<ArgumentException>(() => defaultFactory.Directory(" "));
    }

    [Test]
    public void Should_throw_meaningful_exception_when_requesting_logger_directly_from_default_factory()
    {
        var factory = new TestableDefaultFactory();

        var exception = Assert.Throws<NotSupportedException>(() => factory.ExposeLoggerFactory().GetLogger("SomeLogger"));
        Assert.That(exception!.Message, Does.Contain("LogManager.GetLogger"));
    }

    sealed class TestableDefaultFactory : DefaultFactory
    {
        public global::NServiceBus.Logging.ILoggerFactory ExposeLoggerFactory() => GetLoggingFactory();
    }
}
#pragma warning restore CS0618
