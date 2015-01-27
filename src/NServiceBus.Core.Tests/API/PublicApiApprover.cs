using System.Diagnostics;
using System.IO;
using ApprovalTests;
using ApprovalTests.Core;
using Mono.Cecil;

namespace ApiApprover
{
    public static class PublicApiApprover
    {
        public static void ApprovePublicApi(string assemblyPath, string resultsPath = "")
        {
            var callingMethodFrame = new StackTrace(true).GetFrame(1);
            var sourcePath = Path.GetDirectoryName(callingMethodFrame.GetFileName()) ?? Path.GetTempPath();
            sourcePath = Path.IsPathRooted(resultsPath) ? resultsPath : Path.Combine(sourcePath, resultsPath);

            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

            var readSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"));
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters(ReadingMode.Deferred)
            {
                ReadSymbols = readSymbols,
                AssemblyResolver = assemblyResolver,
            });

            var publicApi = PublicApiGenerator.CreatePublicApiForAssembly(asm);
            var writer = new ApprovalTextWriter(publicApi, "cs");
            var approvalNamer = new AssemblyPathNamer(assemblyPath, sourcePath);
            Approvals.Verify(writer, approvalNamer, Approvals.GetReporter());
        }

        private class AssemblyPathNamer : IApprovalNamer
        {
            public AssemblyPathNamer(string assemblyPath, string sourcePath)
            {
                Name = Path.GetFileNameWithoutExtension(assemblyPath);
                SourcePath = sourcePath;
            }

            public string SourcePath { get; private set; }
            public string Name { get; private set; }
        }
    }
}