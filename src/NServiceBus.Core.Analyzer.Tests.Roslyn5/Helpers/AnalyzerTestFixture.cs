namespace NServiceBus.Core.Analyzer.Tests.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

public partial class AnalyzerTestFixture<TAnalyzer> where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected virtual LanguageVersion AnalyzerLanguageVersion => LanguageVersion.CSharp14;

    protected Task Assert(string markupCode, CancellationToken cancellationToken = default) =>
        Assert([], markupCode, [], true, cancellationToken);

    protected Task Assert(string expectedDiagnosticId, string markupCode, CancellationToken cancellationToken = default) =>
        Assert([expectedDiagnosticId], markupCode, [], true, cancellationToken);

    protected async Task Assert(string[] expectedDiagnosticIds, string markupCode, string[] ignoreDiagnosticIds = null, bool mustCompile = true, CancellationToken cancellationToken = default)
    {
        ignoreDiagnosticIds ??= [];

        var (code, markupSpans) = Parse(markupCode);

        var project = CreateProject(code);
        await WriteCode(project);

        var compilerDiagnostics = (await Task.WhenAll(project.Documents
                .Select(doc => doc.GetCompilerDiagnostics(cancellationToken))))
            .SelectMany(diagnostics => diagnostics);

        WriteCompilerDiagnostics(compilerDiagnostics);

        var compilation = await project.GetCompilationAsync(cancellationToken);
        compilation.Compile(mustCompile);

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
            .Distinct() // in case the analyzer reports the same diagnostic multiple times for the same location. This is possible when reporting at the compilation end too.
            .ToList();

        NUnit.Framework.Assert.That(actualSpansAndIds, Is.EqualTo(expectedSpansAndIds).AsCollection);
    }

    protected static async Task WriteCode(Project project)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

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

    static readonly ImmutableDictionary<string, ReportDiagnostic> DiagnosticOptions = new Dictionary<string, ReportDiagnostic> { { "CS1701", ReportDiagnostic.Hidden } }
        .ToImmutableDictionary();

    protected Project CreateProject(string[] code)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("TestProject", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSpecificDiagnosticOptions(DiagnosticOptions))
            .WithParseOptions(new CSharpParseOptions(AnalyzerLanguageVersion))
            .AddMetadataReferences(AnalyzerTestFixtureState.ProjectReferences);

        for (int i = 0; i < code.Length; i++)
        {
            project = project.AddDocument($"TestDocument{i}", code[i]).Project;
        }

        return project;
    }

    [GeneratedRegex("^-{5,}.*", RegexOptions.Multiline)]
    private static partial Regex DocumentSplittingRegex();

    protected static void WriteCompilerDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

        Console.WriteLine("Compiler diagnostics:");

        LogAnalyzerDiagnostics(diagnostics);
    }

    protected static void WriteAnalyzerDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        if (!AnalyzerTestFixtureState.VerboseLogging)
        {
            return;
        }

        Console.WriteLine("Analyzer diagnostics:");

        LogAnalyzerDiagnostics(diagnostics);
    }

    static void LogAnalyzerDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        foreach (var byTags in diagnostics.GroupBy(d => d.Descriptor.CustomTags))
        {
            var tags = string.Join(", ", byTags.Key);
            Console.WriteLine($"  Tags: {tags}");
            foreach (var diagnostic in byTags)
            {
                Console.WriteLine($"    {diagnostic}");
            }
        }
    }

    protected static string[] SplitMarkupCodeIntoFiles(string markupCode) =>
        [.. DocumentSplittingRegex().Split(markupCode)
            .Where(docCode => !string.IsNullOrWhiteSpace(docCode))];

    static (string[] code, List<(string file, TextSpan span)>) Parse(string markupCode)
    {
        if (markupCode == null)
        {
            return ([], []);
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
                var beforeAndAfterOpening = remainingCode.Split(AnalyzerTestFixtureState.OpeningSeparator, 2, StringSplitOptions.None);

                if (beforeAndAfterOpening.Length == 1)
                {
                    _ = code.Append(beforeAndAfterOpening[0]);
                    break;
                }

                var midAndAfterClosing = beforeAndAfterOpening[1].Split(AnalyzerTestFixtureState.ClosingSeparator, 2, StringSplitOptions.None);

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