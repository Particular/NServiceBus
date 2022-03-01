namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using NServiceBus.UniformSession;

    public class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
    {
        protected Task Assert(string markupCode, CancellationToken cancellationToken = default) =>
            Assert(Array.Empty<string>(), markupCode, Array.Empty<string>(), cancellationToken);

        protected Task Assert(string expectedDiagnosticId, string markupCode, CancellationToken cancellationToken = default) =>
            Assert(new[] { expectedDiagnosticId }, markupCode, Array.Empty<string>(), cancellationToken);

        protected async Task Assert(string[] expectedDiagnosticIds, string markupCode, string[] ignoreDiagnosticIds, CancellationToken cancellationToken = default)
        {
            var (code, markupSpans) = Parse(markupCode);

            var project = CreateProject(code);
            await WriteCode(project);

            var compilerDiagnostics = (await Task.WhenAll(project.Documents
                .Select(doc => doc.GetCompilerDiagnostics(cancellationToken))))
                .SelectMany(diagnostics => diagnostics);

            WriteCompilerDiagnostics(compilerDiagnostics);

            var compilation = await project.GetCompilationAsync(cancellationToken);
            compilation.Compile();

            var analyzerDiagnostics = (await compilation.GetAnalyzerDiagnostics(new TAnalyzer(), cancellationToken))
                .Where(d => !ignoreDiagnosticIds.Contains(d.Id))
                .ToList();
            WriteAnalyzerDiagnostics(analyzerDiagnostics);

            var expectedSpansAndIds = expectedDiagnosticIds
                .SelectMany(id => markupSpans.Select(span => (span.file, span.span, id)))
                .OrderBy(item => item.span)
                .ThenBy(item => item.id)
                .ToList();

            var actualSpansAndIds = analyzerDiagnostics
                .Select(diagnostic => (diagnostic.Location.SourceTree.FilePath, diagnostic.Location.SourceSpan, diagnostic.Id))
                .ToList();

            NUnit.Framework.CollectionAssert.AreEqual(expectedSpansAndIds, actualSpansAndIds);
        }

        protected static async Task WriteCode(Project project)
        {
            foreach (var document in project.Documents)
            {
                Console.WriteLine(document.Name);
                var code = await document.GetCode();
                foreach (var (line, index) in code.Replace("\r\n", "\n").Split('\n')
                .Select((line, index) => (line, index)))
                {
                    Console.WriteLine($"  {index + 1,3}: {line}");
                }
            }

        }

        static readonly ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions = new Dictionary<string, ReportDiagnostic>
        {
            { "CS1701", ReportDiagnostic.Hidden }
        }
        .ToImmutableDictionary();

        protected static Project CreateProject(string[] code)
        {
            var references = ImmutableList.Create(
                MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).GetTypeInfo().Assembly.Location),
#if NETCOREAPP
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
#endif
                MetadataReference.CreateFromFile(typeof(EndpointConfiguration).GetTypeInfo().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IUniformSession).GetTypeInfo().Assembly.Location));

            var workspace = new AdhocWorkspace();
            var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithSpecificDiagnosticOptions(DiagnosticOptions))
                .WithParseOptions(new CSharpParseOptions(LanguageVersion.CSharp8))
                .AddMetadataReferences(references);

            for (int i = 0; i < code.Length; i++)
            {
                project = project.AddDocument($"TestDocument{i}", code[i]).Project;
            }

            return project;
        }

        static readonly Regex DocumentSplittingRegex = new Regex("^-{5,}.*", RegexOptions.Compiled | RegexOptions.Multiline);

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

        protected static string[] SplitMarkupCodeIntoFiles(string markupCode)
        {
            return DocumentSplittingRegex.Split(markupCode)
                .Where(docCode => !string.IsNullOrWhiteSpace(docCode))
                .ToArray();
        }

        static (string[] code, List<(string file, TextSpan span)>) Parse(string markupCode)
        {
            if (markupCode == null)
            {
                return (Array.Empty<string>(), new List<(string, TextSpan)>());
            }

            var documents = SplitMarkupCodeIntoFiles(markupCode);

            var markupSpans = new List<(string, TextSpan)>();

            for (var i = 0; i < documents.Length; i++)
            {
                var code = new StringBuilder();
                var name = $"TestDocument{i}";

                var remainingCode = documents[i];
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
                    markupSpans.Add((name, markupSpan));

                    remainingCode = midAndAfterClosing[1];
                    remainingCodeStart += beforeAndAfterOpening[0].Length + markupSpan.Length;
                }

                documents[i] = code.ToString();
            }

            return (documents, markupSpans);
        }
    }
}
