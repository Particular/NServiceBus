namespace NServiceBus.Core.Tests.Logging;

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
        Assert.That(nonExistingDirectoryException.Message, Is.EqualTo("Could not find logging directory: 'baddir'"));
        Assert.Throws<ArgumentNullException>(() => defaultFactory.Directory(null));
        Assert.Throws<ArgumentException>(() => defaultFactory.Directory(""));
        Assert.Throws<ArgumentException>(() => defaultFactory.Directory(" "));
    }
}
