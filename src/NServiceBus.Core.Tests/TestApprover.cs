namespace NServiceBus.Core.Tests
{
    using System.IO;
    using ApprovalTests;
    using ApprovalTests.Namers;
    using NUnit.Framework;

    static class TestApprover
    {
        public static void Verify(string text)
        {
            var writer = new ApprovalTextWriter(text);
            var namer = new ApprovalNamer();
            Approvals.Verify(writer, namer, Approvals.GetReporter());
        }

        class ApprovalNamer : UnitTestFrameworkNamer
        {
            public override string SourcePath { get; } = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "ApprovalFiles");
        }
    }
}
