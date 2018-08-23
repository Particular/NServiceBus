namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using Microsoft.CodeAnalysis;

    public class DiagnosticResult
    {
        public DiagnosticResultLocation[] Locations { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }
    }
}
