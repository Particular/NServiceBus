namespace NServiceBus.Core.Tests
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Core;
    using ApprovalTests.Namers.StackTraceParsers;
    using NUnit.Framework;

    static class TestApprover
    {
        public static void Verify(string text)
        {
            var writer = new ApprovalTextWriter(text);
            var namer = new ApprovalNamer();
            Approvals.Verify(writer, namer, Approvals.GetReporter());
        }

        class ApprovalNamer : IApprovalNamer
        {
            public ApprovalNamer()
            {
                Approvals.SetCaller();
                stackTraceParser = new StackTraceParser();
                stackTraceParser.Parse(Approvals.CurrentCaller.StackTrace);
            }

            public string Name => stackTraceParser.ApprovalName;

            public string SourcePath { get; } = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "ApprovalFiles");

            readonly StackTraceParser stackTraceParser;
        }
    }
}
