namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_for_dlls_only
    {
        static readonly string BaseDirectoryToScan = Path.Combine(Path.GetTempPath(), "empty");

        AssemblyScannerResults results;

        [SetUp]
        public void Context()
        {
            Directory.CreateDirectory(BaseDirectoryToScan);

            var dllFilePath = Path.Combine(BaseDirectoryToScan, "NotAProper.exe");
            File.WriteAllText(dllFilePath, "This is not a proper EXE");

            results = new AssemblyScanner(BaseDirectoryToScan)
                {
                    IncludeAppDomainAssemblies = true,
                    IncludeExesInScan = false,
                }
                .GetScannableAssemblies();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(BaseDirectoryToScan))
                Directory.Delete(BaseDirectoryToScan, true);
        }

        [Test]
        public void should_not_find_assembly_in_sub_directory()
        {
            var allEncounteredFileNames =
                results.Assemblies
                    .Where(x => !x.IsDynamic)
                    .Select(a => a.CodeBase)
                    .Concat(results.SkippedFiles.Select(s => s.FilePath))
                    .ToList();

            Assert.That(allEncounteredFileNames.Any(f => f.Contains("NotAProper.exe")),
                Is.False, "Did not expect to find NotAProper.dll among all encountered files because it resides in a sub directory");
        }
    }
}