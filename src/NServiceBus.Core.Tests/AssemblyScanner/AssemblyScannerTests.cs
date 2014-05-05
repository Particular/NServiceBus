namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AssemblyScannerTests
    {

        [Test]
        public void System_assemblies_should_be_excluded()
        {
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(string).Assembly.Location));
            Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(typeof(Uri).Assembly.Location));
        }

        [Test]
        public void Non_system_assemblies_should_be_included()
        {
            Assert.IsFalse(AssemblyScanner.IsRuntimeAssembly(GetType().Assembly.Location));
        }
    }
}