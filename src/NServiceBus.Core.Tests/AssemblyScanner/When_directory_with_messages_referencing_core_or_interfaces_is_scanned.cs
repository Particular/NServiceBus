namespace NServiceBus.Core.Tests.AssemblyScanner;

using System.IO;
using System.Linq;
using Hosting.Helpers;
using NUnit.Framework;

[TestFixture]
public class When_directory_with_messages_referencing_core_or_interfaces_is_scanned
{
    [Test]
    public void Assemblies_should_be_scanned()
    {
        var scanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls", "Messages"))
        {
            ScanAppDomainAssemblies = false
        };

        var result = scanner.GetScannableAssemblies();
        var assemblyFullNames = result.Assemblies.Select(a => a.GetName().Name).ToList();

        Assert.That(assemblyFullNames, Has.Member("Messages.Referencing.Core"));
        Assert.That(assemblyFullNames, Has.Member("Messages.Referencing.MessageInterfaces"));
    }
}