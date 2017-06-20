#if NET452
namespace NServiceBus.Core.Tests.AssemblyScanner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_exclusion_predicate_is_used
    {

        [Test]
        public void no_files_explicitly_excluded_are_returned()
        {
            var results = new AssemblyScanner(Path.Combine(TestContext.CurrentContext.TestDirectory, "TestDlls"))
                          {
                              AssembliesToSkip = new List<string>
                                                 {
                                                     "dotNet.dll"
                                                 }
                          }
                .GetScannableAssemblies();

            var skippedFiles = results.SkippedFiles;
            var explicitlySkippedDll = skippedFiles.FirstOrDefault(s => s.FilePath.Contains("dotNet.dll"));

            Assert.That(explicitlySkippedDll, Is.Not.Null);
            Assert.That(explicitlySkippedDll.SkipReason, Contains.Substring("File was explicitly excluded from scanning"));
        }
    }
}
#endif