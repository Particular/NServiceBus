namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_system_assemblies
    {
#if NET452
        [Test]
        public void Should_exclude_system_assemblies_on_net_framework()
        {
            var systemAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("System"));

            foreach (var assembly in systemAssemblies)
            {
                Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(assembly.GetName()), $"should exclude {assembly.FullName}");
            }
        }
#endif

#if NETCOREAPP2_0
        [Test]
        public void Should_exclude_system_assemblies_on_net_core()
        {
            var systemAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("System"));

            foreach (var assembly in systemAssemblies)
            {
                Assert.IsTrue(AssemblyScanner.IsRuntimeAssembly(assembly.GetName()), $"should exclude {assembly.FullName}");
            }
        }
#endif

        [Test]
        public void NServiceBus_core_should_be_included()
        {
            Assert.IsFalse(AssemblyScanner.IsRuntimeAssembly(typeof(EndpointConfiguration).Assembly.GetName()));
        }

        [Test]
        public void Non_system_assemblies_should_be_included()
        {
            Assert.IsFalse(AssemblyScanner.IsRuntimeAssembly(GetType().Assembly.GetName()));
        }
    }
}