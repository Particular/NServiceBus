namespace NServiceBus.Testing.Tests
{
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;

    public abstract class BaseTests
    {
        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            Test.Initialize(b => b.ScanAssembliesInDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));
        }
    }
}