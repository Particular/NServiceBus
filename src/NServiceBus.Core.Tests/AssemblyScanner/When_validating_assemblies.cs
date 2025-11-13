namespace NServiceBus.Core.Tests.AssemblyScanner;

using System;
using System.Linq;
using NUnit.Framework;

[TestFixture]
public class When_validating_assemblies
{
    [Test]
    public void Should_not_validate_system_assemblies()
    {
        var systemAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("System"));

        foreach (var assembly in systemAssemblies)
        {
            AssemblyValidator.ValidateAssemblyFile(assembly.Location, out var shouldLoad, out var reason);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(shouldLoad, Is.False, $"Should not validate {assembly.FullName}");
                Assert.That(reason, Is.EqualTo("File is a .NET runtime assembly."));
            }
        }
    }

    [Test]
    public void Should_not_validate_NServiceBus_Core_assembly()
    {
        AssemblyValidator.ValidateAssemblyFile(typeof(EndpointConfiguration).Assembly.Location, out var shouldLoad, out var reason);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(shouldLoad, Is.False);
            Assert.That(reason, Is.EqualTo("File is a Particular assembly."));
        }
    }

    [Test]
    public void Should_validate_non_system_assemblies()
    {
        AssemblyValidator.ValidateAssemblyFile(GetType().Assembly.Location, out var shouldLoad, out var reason);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(shouldLoad, Is.True);
            Assert.That(reason, Is.EqualTo("File is a .NET assembly."));
        }
    }
}