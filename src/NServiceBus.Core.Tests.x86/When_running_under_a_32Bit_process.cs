namespace NServiceBus.Core.Tests.x32
{
    using System.Linq;
    using Hosting.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class When_running_under_a_32Bit_process
    {
        [Test]
        public void Should_skip_x64_assemblies()
        {
            var results = new AssemblyScanner()
                .GetScannableAssemblies();

            var x64SkippedDll = results.SkippedFiles.FirstOrDefault(s => s.FilePath.Contains("x64Assembly.dll"));

            Assert.That(x64SkippedDll, Is.Not.Null);
            Assert.That(x64SkippedDll.SkipReason, Contains.Substring("x64 .NET assembly can't be loaded by a 32Bit process"));
        }
    }
}
