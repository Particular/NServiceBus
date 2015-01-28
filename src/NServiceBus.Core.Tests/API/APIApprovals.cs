namespace NServiceBus.Core.Tests.API
{
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using ApprovalTests.Namers;
    using ApprovalTests.Reporters;
    using NUnit.Framework;

    [UseReporter(typeof(DiffReporter))]
    [TestFixture]
    public class APIApprovals
    {
        [Ignore("Lots of problems with whitespace")]
        [Test]
        [TestCaseSource("AssemblyPaths")]
        [UseApprovalSubdirectory("approvals")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void approve_public_api(string assembly, string path)
        {
            PublicApiApprover.ApprovePublicApi(Path.Combine(path, assembly));
        }

        public static IEnumerable<TestCaseData> AssemblyPaths
        {
            get
            {
                yield return ArgsFor<IBus>();
            }
        }

        private static TestCaseData ArgsFor<T>()
        {
            var path = Path.GetFullPath(typeof(T).Assembly.Location);

            return new TestCaseData(Path.GetFileName(path), Path.GetDirectoryName(path));
        }
    }
}