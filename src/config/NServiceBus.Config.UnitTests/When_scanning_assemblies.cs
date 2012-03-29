namespace NServiceBus.Config.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;

    [TestFixture]
    public class When_finding_assemblies_to_scan
    {
        List<Assembly> foundAssemblies;

        [SetUp]
        public void SetUp()
        {
            foundAssemblies = Configure.GetAssembliesInDirectory(AppDomain.CurrentDomain.BaseDirectory)
                .ToList();
        }

        [Test]
        public void Should_exclude_system_assemblies()
        {
            Assert.False(
                foundAssemblies.Any(a => a.FullName.StartsWith("System")));
        }

        [Test]
        public void Should_exclude_nhibernate_assemblies()
        {
            Assert.False(
                foundAssemblies.Any(a => a.FullName.ToLower().StartsWith("nhibernate")));
        }

        [Test]
        public void Should_exclude_log4net()
        {
            Assert.False(
                foundAssemblies.Any(a => a.FullName.ToLower().StartsWith("log4net")));
        }

        [Test]
        public void Should_exclude_raven()
        {
            Assert.False(
                foundAssemblies.Any(a => a.FullName.ToLower().StartsWith("raven")));
        }
    }
}