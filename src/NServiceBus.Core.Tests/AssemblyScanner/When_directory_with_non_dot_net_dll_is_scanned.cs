namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_directory_with_non_dot_net_dll_is_scanned
    {
        [Test]
        public void non_dotnet_files_are_skipped()
        {
            var assemblyScanner = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls"));
            assemblyScanner.ScanAppDomainAssemblies = false;

            var results = assemblyScanner
                .GetScannableAssemblies();

            var skippedFiles = results.SkippedFiles;

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
                    throw new AssertionException($"Could not find skipped file matching {notProperDll}");
                }

                Assert.That(skippedFile.SkipReason, Contains.Substring("not a .NET assembly"));
            }
        }

    }
}