namespace NServiceBus.Core.Tests.AssemblyScanner;

using System.IO;
using System.Reflection;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_directory_with_messages_referencing_core_is_scanned
{
    [Test]
    public void Assemblies_referencing_core_should_be_scanned()
    {
        var assemblyToScan = Assembly.LoadFrom(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages.Referencing.Core.dll"));
        var scanner = new AssemblyScanner(assemblyToScan);

        var result = scanner.GetScannableAssemblies();

        Assert.That(result.Assemblies.Contains(assemblyToScan), Is.True);
    }
}