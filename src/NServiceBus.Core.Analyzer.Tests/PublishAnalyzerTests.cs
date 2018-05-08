using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NServiceBus.Core.Analyzer.Tests.Helpers;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NServiceBus.Core.Analyzer.Tests
{
    [TestFixture]
    public class PublishAnalyzerTests : DiagnosticVerifier
    {
        [Test]
        public void NoCode() => Verify("");

        [Test]
        public async Task Publish()
        {
            var source = @"some code";

            var expected = new DiagnosticResult
            {
                Id = "NServiceBus.Core.001",
                Message = "TBD",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 1) },
            };

            await Verify(source, expected);
        }

        protected override DiagnosticAnalyzer GetAnalyzer() => new PublishAnalyzer();
    }
}
