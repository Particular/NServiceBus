namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using NServiceBus.UniformSession;

    public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        static readonly string[] EmptyStringArray = new string[0];

        protected Task Assert(string markupCode, CancellationToken cancellationToken = default) =>
            Assert(EmptyStringArray, markupCode, cancellationToken);

        protected Task Assert(string expectedDiagnosticId, string markupCode, CancellationToken cancellationToken = default) =>
            Assert(new[] { expectedDiagnosticId }, markupCode, cancellationToken);

        protected async Task Assert(string[] expectedDiagnosticIds, string markupCode, CancellationToken cancellationToken = default)
        {
            var code = Parse(markupCode, out var markupSpans);
            WriteCode(code);

            var document = CreateDocument(code);

            var compilerDiagnostics = await document.GetCompilerDiagnostics(cancellationToken);
            WriteCompilerDiagnostics(compilerDiagnostics);

            var compilation = await document.Project.GetCompilationAsync(cancellationToken);
            compilation.Compile();

            var analyzerDiagnostics = (await compilation.GetAnalyzerDiagnostics(new TAnalyzer(), cancellationToken)).ToList();
            WriteAnalyzerDiagnostics(analyzerDiagnostics);

            var expectedSpansAndIds = expectedDiagnosticIds
                .SelectMany(id => markupSpans.Select(span => new Tuple<TextSpan, string>(span, id)))
                .OrderBy(item => item.Item1)
                .ThenBy(item => item.Item2)
                .ToList();

            var actualSpansAndIds = analyzerDiagnostics
                .Select(diagnostic => new Tuple<TextSpan, string>(diagnostic.Location.SourceSpan, diagnostic.Id))
                .ToList();

            NUnit.Framework.CollectionAssert.AreEqual(expectedSpansAndIds, actualSpansAndIds);
        }

        protected static void WriteCode(string code)
        {
            foreach (var lineAndIndex in code.Replace("\r\n", "\n").Split('\n')
                .Select((line, index) => new Tuple<string, int>(line, index)))
            {
                Console.WriteLine($"  {lineAndIndex.Item2 + 1,3}: {lineAndIndex.Item1}");
            }
        }

        protected static Document CreateDocument(string code)
        {
            var references = ImmutableList.Create(
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
#if NETCOREAPP
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
#endif
                MetadataReference.CreateFromFile(typeof(EndpointConfiguration).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IUniformSession).GetTypeInfo().Assembly.Location));

            return new AdhocWorkspace()
                .AddProject("TestProject", LanguageNames.CSharp)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddMetadataReferences(references)
                .AddDocument("TestDocument", code);
        }

        protected static void WriteCompilerDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            Console.WriteLine("Compiler diagnostics:");

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"  {diagnostic}");
            }
        }

        protected static void WriteAnalyzerDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            Console.WriteLine("Analyzer diagnostics:");

            foreach (var diagnostic in diagnostics)
            {
                Console.WriteLine($"  {diagnostic}");
            }
        }

        static string Parse(string markupCode, out List<TextSpan> markupSpans)
        {
            markupSpans = new List<TextSpan>();

            if (markupCode == null)
            {
                return null;
            }

            var code = new StringBuilder();

            var remainingCode = markupCode;
            var remainingCodeStart = 0;

            while (remainingCode.Length > 0)
            {
                var beforeAndAfterOpening = remainingCode.Split(new[] { "[|" }, 2, StringSplitOptions.None);

                if (beforeAndAfterOpening.Length == 1)
                {
                    _ = code.Append(beforeAndAfterOpening[0]);
                    break;
                }

                var midAndAfterClosing = beforeAndAfterOpening[1].Split(new[] { "|]" }, 2, StringSplitOptions.None);

                if (midAndAfterClosing.Length == 1)
                {
                    throw new Exception("The markup code does not contain a closing '|]'");
                }

                var markupSpan = new TextSpan(remainingCodeStart + beforeAndAfterOpening[0].Length, midAndAfterClosing[0].Length);

                _ = code.Append(beforeAndAfterOpening[0]).Append(midAndAfterClosing[0]);
                markupSpans.Add(markupSpan);

                remainingCode = midAndAfterClosing[1];
                remainingCodeStart += beforeAndAfterOpening[0].Length + markupSpan.Length;
            }

            return code.ToString();
        }
    }
}
