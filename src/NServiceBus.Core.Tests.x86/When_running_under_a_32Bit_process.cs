namespace NServiceBus.Core.Tests.x32
{
    using System.Collections.Generic;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_running_under_a_32Bit_process
    {
        AssemblyScannerResults results;
        List<SkippedFile> skippedFiles;

        [SetUp]
        public void Context()
        {
            results = new AssemblyScanner()
                .GetScannableAssemblies();

            skippedFiles = results.SkippedFiles;
        }

        [Test]
        public void Should_skip_x64_assemblies_automagically()
        {
            var x64SkippedDll = skippedFiles.FirstOrDefault(s => s.FilePath.Contains("ConventionBasedHandler.Tests.dll"));

            Assert.That(x64SkippedDll, Is.Not.Null);
            Assert.That(x64SkippedDll.SkipReason, Contains.Substring("x64 .NET assembly can't be loaded by a 32Bit process"));
        }
    }
}
