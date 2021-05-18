namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_scanning_top_level_only
    {
        static string baseDirectoryToScan = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "empty");
        static string someSubDirectory = Path.Combine(baseDirectoryToScan, "subDir");

        [SetUp]
        public void Context()
        {
            Directory.CreateDirectory(baseDirectoryToScan);
            Directory.CreateDirectory(someSubDirectory);

            var dllFilePath = Path.Combine(someSubDirectory, "NotAProper.dll");
            File.WriteAllText(dllFilePath, "This is not a proper DLL");
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(baseDirectoryToScan))
            {
                Directory.Delete(baseDirectoryToScan, true);
            }
        }

        [Test]
        public void Should_not_find_assembly_in_sub_directory()
        {
            var results = new AssemblyScanner(baseDirectoryToScan)
            {
                ScanAppDomainAssemblies = false,
                ScanNestedDirectories = false
            }
            .GetScannableAssemblies();

            var allEncounteredFileNames =
                results.Assemblies
                    .Where(x => !x.IsDynamic)
                    .Select(a => a.Location)
                    .Concat(results.SkippedFiles.Select(s => s.FilePath))
                    .ToList();

            Assert.That(allEncounteredFileNames.Any(f => f.Contains("NotAProper.dll")),
                Is.False, "Did not expect to find NotAProper.dll among all encountered files because it resides in a sub directory");
        }
    }
}