namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_exclusion_predicate_is_used
    {
        AssemblyScannerResults results;
        List<SkippedFile> skippedFiles;

        [SetUp]
        public void Context()
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            var directoryName = Path.GetDirectoryName(path);

            var testDllDirectory = Path.Combine(directoryName, "TestDlls");
            results = new AssemblyScanner(testDllDirectory)
                {
                    AssembliesToSkip = new List<string>
                        {
                            "dotNet.dll"
                        }
                }
                .GetScannableAssemblies();

            skippedFiles = results.SkippedFiles;
        }

        [Test]
        public void no_files_explicitly_excluded_are_returned()
        {
            var explicitlySkippedDll = skippedFiles.FirstOrDefault(s => s.FilePath.Contains("dotNet.dll"));

            Assert.That(explicitlySkippedDll, Is.Not.Null);
            Assert.That(explicitlySkippedDll.SkipReason, Contains.Substring("File was explicitly excluded from scanning"));
        }
    }
}