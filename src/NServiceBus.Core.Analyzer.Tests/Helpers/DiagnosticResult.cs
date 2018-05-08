namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using Microsoft.CodeAnalysis;

    public class DiagnosticResult
    {
        public DiagnosticResultLocation[] Locations { get; set; }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path => this.Locations?.Length > 0 ? this.Locations[0].Path : "";

        public int Line => this.Locations?.Length > 0 ? this.Locations[0].Line : -1;

        public int Column => this.Locations?.Length > 0 ? this.Locations[0].Character : -1;
    }
}
