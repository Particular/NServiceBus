namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class When_validating_assemblies
    {
        [Test]
        public void Should_not_validate_system_assemblies()
        {
            var validator = new AssemblyValidator();
            var systemAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("System"));

            foreach (var assembly in systemAssemblies)
            {
                var (shouldLoad, reason) = validator.ValidateAssemblyFile(assembly.Location);

                Assert.IsFalse(shouldLoad, $"Should not validate {assembly.FullName}");
                Assert.That(reason == "File is a .NET runtime assembly.");
            }
        }

        [Test]
        public void Should_validate_NServiceBus_Core_assembly()
        {
            var validator = new AssemblyValidator();

            var (shouldLoad, reason) = validator.ValidateAssemblyFile(typeof(EndpointConfiguration).Assembly.Location);

            Assert.IsTrue(shouldLoad);
            Assert.That(reason == "File is a .NET assembly.");
        }

        [Test]
        public void Should_validate_non_system_assemblies()
        {
            var validator = new AssemblyValidator();

            var (shouldLoad, reason) = validator.ValidateAssemblyFile(GetType().Assembly.Location);

            Assert.IsTrue(shouldLoad);
            Assert.That(reason == "File is a .NET assembly.");
        }
    }
}