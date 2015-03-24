namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Collections.Generic;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_no_reference_dlls_is_scanned
    {
        List<SkippedFile> skippedFiles;

        [SetUp]
        public void Context()
        {
            var assemblyScanner = new AssemblyScanner(AssemblyScannerTests.GetTestAssemblyDirectory())
                {
                    IncludeAppDomainAssemblies = false
                };

            var results = assemblyScanner
                .GetScannableAssemblies();

            skippedFiles = results.SkippedFiles;
        }

        [Test]
        [Explicit("TODO: re-enable when we make message scanning lazy #1617")]
        public void assemblies_without_nsb_reference_are_skipped()
        {
            var skippedFile = skippedFiles.FirstOrDefault(f => f.FilePath.Contains("dotNet.dll"));

            if (skippedFile == null)
            {
                throw new AssertionException(string.Format("Could not find skipped file matching {0}", "dotNet.dll"));
            }
            Assert.That(skippedFile.SkipReason,
                Contains.Substring("Assembly does not reference at least one of the must referenced assemblies"));
        }

    }
}