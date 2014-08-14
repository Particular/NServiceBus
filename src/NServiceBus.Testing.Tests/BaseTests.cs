namespace NServiceBus.Testing.Tests
{
    using System;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;

    public abstract class BaseTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            var directoryName = Path.GetDirectoryName(path);

            Address.preventChanges = false;
            Test.Initialize(b => b.ScanAssembliesInDirectory(directoryName));
        }
    }
}