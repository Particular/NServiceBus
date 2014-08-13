namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory__with_non_dot_net_dll_is_scanned
    {
        AssemblyScannerResults results;
        List<SkippedFile> skippedFiles;

        [SetUp]
        public void Context()
        {
            var testDllDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "TestDlls");
            var assemblyScanner = new AssemblyScanner(testDllDirectory)
                {
                    IncludeAppDomainAssemblies = false
                };
            assemblyScanner.MustReferenceAtLeastOneAssembly.Add(typeof(IHandleMessages<>).Assembly);

            results = assemblyScanner
                .GetScannableAssemblies();

            skippedFiles = results.SkippedFiles;
        }

        [Test]
        public void non_dotnet_files_are_skipped()
        {
            var notProperDotNetDlls = new[]
                {
                    "libzmq-v120-mt-3_2_3.dll",
                    "Tail.exe",
                    "some_random.dll",
                    "some_random.exe"
                };

            foreach (var notProperDll in notProperDotNetDlls)
            {
                var skippedFile = skippedFiles.FirstOrDefault(f => f.FilePath.Contains(notProperDll));

                if (skippedFile == null)
                {
                    throw new AssertionException(string.Format("Could not find skipped file matching {0}", notProperDll));
                }

                Assert.That(skippedFile.SkipReason, Contains.Substring("not a .NET assembly"));
            }
        }

    }
}