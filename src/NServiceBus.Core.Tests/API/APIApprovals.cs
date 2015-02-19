namespace NServiceBus.Core.Tests.API
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using ApprovalTests;
    using ApprovalTests.Namers;
    using ApprovalTests.Reporters;
    using Mono.Cecil;
    using NUnit.Framework;

    [UseReporter(typeof(DiffReporter))]
    [TestFixture]
    public class APIApprovals
    {
        [Ignore("We reenable when we fix the newline issue")]
        [Test]
        [TestCaseSource("AssemblyPaths")]
        [UseApprovalSubdirectory("approvals")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void approve_public_api(string assembly, string path)
        {
            ApprovePublicApi(Path.Combine(path, assembly), Filter);
        }

        public static IEnumerable<TestCaseData> AssemblyPaths
        {
            get
            {
                yield return ArgsFor<IBus>();
            }
        }

        private static string Filter(string text)
        {
            return string.Join(Environment.NewLine, text.Split(new[]
            {
                Environment.NewLine
            }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("[assembly: ReleaseDateAttribute("))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                );
        }

        private static TestCaseData ArgsFor<T>()
        {
            var path = Path.GetFullPath(typeof(T).Assembly.Location);

            return new TestCaseData(Path.GetFileName(path), Path.GetDirectoryName(path));
        }

        private static void ApprovePublicApi(string assemblyPath, Func<string, string> stringFormatter)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

            var readSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"));
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters(ReadingMode.Deferred)
            {
                ReadSymbols = readSymbols,
                AssemblyResolver = assemblyResolver,
            });

            var publicApi = stringFormatter(PublicApiGenerator.CreatePublicApiForAssembly(asm));
            var writer = new ApprovalTextWriter(publicApi, "cs");
            var approvalNamer = new AssemblyPathNamer(assemblyPath);
            Approvals.Verify(writer, approvalNamer, Approvals.GetReporter());
        }

        private class AssemblyPathNamer : UnitTestFrameworkNamer
        {
            private readonly string name;

            public AssemblyPathNamer(string assemblyPath)
            {
                name = Path.GetFileNameWithoutExtension(assemblyPath);
            }

            public override string Name
            {
                get { return name; }
            }
        }
    }
}