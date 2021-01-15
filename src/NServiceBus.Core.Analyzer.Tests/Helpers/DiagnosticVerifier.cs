namespace NServiceBus.Core.Analyzer.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;
    using NUnit.Framework;
    using UniformSession;

    public abstract class DiagnosticVerifier
    {
        static readonly MetadataReference CorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        static readonly MetadataReference SystemCoreReference = MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);
        static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        static readonly MetadataReference CodeAnalysisReference = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        static readonly MetadataReference NServiceBusReference = MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location);
        static readonly MetadataReference TestLib = MetadataReference.CreateFromFile(typeof(IUniformSession).Assembly.Location);

        protected abstract DiagnosticAnalyzer GetAnalyzer();

        protected Task Verify(string source, params DiagnosticResult[] expectedResults) => Verify(new[] { source }, expectedResults);

        async Task Verify(string[] sources, params DiagnosticResult[] expectedResults)
        {
            var analyzer = GetAnalyzer();

            var actualResults = await GetSortedDiagnostics(sources, analyzer);

            var expectedCount = expectedResults.Length;
            var actualCount = actualResults.Length;

            if (expectedCount != actualCount)
            {
                var diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                Assert.Fail($"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
            }

            for (var expectedResultIndex = 0; expectedResultIndex < expectedResults.Length; expectedResultIndex++)
            {
                var actual = actualResults.ElementAt(expectedResultIndex);
                var expected = expectedResults[expectedResultIndex];

                if (!expected.Locations.Any() && actual.Location != Location.None)
                {
                    Assert.Fail($"Expected:\nA project diagnostic with no location\nActual:\n{FormatDiagnostics(analyzer, actual)}");
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());

                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        Assert.Fail($"Expected {expected.Locations.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
                    }

                    for (var additionalLocationIndex = 0; additionalLocationIndex < additionalLocations.Length; ++additionalLocationIndex)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[additionalLocationIndex], expected.Locations[additionalLocationIndex + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    Assert.Fail($"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
                }

                if (actual.Severity != expected.Severity)
                {
                    Assert.Fail($"Expected diagnostic severity to be \"{expected.Severity}\" was \"{actual.Severity}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, actual)}\r\n");
                }
            }
        }

        static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            Assert.IsTrue(
                actualSpan.Path == expected.Path || (actualSpan.Path != null && actualSpan.Path.Contains("Test0.") && expected.Path.Contains("Test.")),
                $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");

            var actualPosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualPosition.Line > 0 && actualPosition.Line + 1 != expected.Line)
            {
                Assert.Fail($"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualPosition.Line + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
            }

            // Only check character position if there is an actual character position in the real diagnostic
            if (actualPosition.Character > 0 && actualPosition.Character + 1 != expected.Character)
            {
                Assert.Fail($"Expected diagnostic to start at character \"{expected.Character}\" was actually at character \"{actualPosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer, diagnostic)}\r\n");
            }
        }

        protected Task<Diagnostic[]> GetSortedDiagnostics(string[] sources, DiagnosticAnalyzer analyzer)
        {
            Project newProject = CreateProject(sources);

            var documents = newProject.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new InvalidOperationException("The number of documents created does not match the number of sources.");
            }

            return GetSortedDiagnosticsFromDocuments(analyzer, documents);
        }

        protected async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var project in new HashSet<Project>(documents.Select(document => document.Project)))
            {
                var compilation = await project.GetCompilationAsync();
                var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));

                using (var stream = new System.IO.MemoryStream())
                {
                    var emitResult = compilation.Emit(stream);
                    if (!emitResult.Success)
                    {
                        foreach (var diagnostic in emitResult.Diagnostics)
                        {
                            Console.WriteLine(diagnostic.Location.GetMappedLineSpan() + " " + diagnostic.GetMessage());
                        }
                        throw new Exception("Test code did not compile.");
                    }
                }

                foreach (var diagnostic in await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync())
                {
                    if (diagnostic.Location == Location.None || diagnostic.Location.IsInMetadata)
                    {
                        diagnostics.Add(diagnostic);
                    }
                    else
                    {
                        foreach (var document in documents)
                        {
                            if (await document.GetSyntaxTreeAsync() == diagnostic.Location.SourceTree)
                            {
                                diagnostics.Add(diagnostic);
                            }
                        }
                    }
                }
            }

            return diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start).ToArray();
        }

        protected Project CreateProject(params string[] sources)
        {
            var projectId = ProjectId.CreateNewId("TestProject");

            var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
                .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // The max C# version is controlled by the Roslyn package in use - that's what's used to parse the code in tests
            var parseOptions = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp6);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectInfo)
                .AddMetadataReference(projectId, CorlibReference)
                .AddMetadataReference(projectId, SystemCoreReference)
                .AddMetadataReference(projectId, CSharpSymbolsReference)
                .AddMetadataReference(projectId, CodeAnalysisReference)
                .AddMetadataReference(projectId, TestLib)
                .AddMetadataReference(projectId, NServiceBusReference)
                .WithProjectParseOptions(projectId, parseOptions);

#if NETCOREAPP
            var netstandard = MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("netstandard").Location);
            var systemTasks = MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Threading.Tasks").Location);
            var systemRuntime = MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location);
            solution = solution.AddMetadataReferences(projectId, new[]
            {
                netstandard,
                systemRuntime,
                systemTasks
            });
#endif
            var documentIndex = 0;
            foreach (var source in sources)
            {
                var fileName = "Test" + documentIndex + ".cs";
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId, fileName), fileName, SourceText.From(source));
                documentIndex++;
            }

            var project = solution.GetProject(projectId);
            return project;
        }

        static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (var diagnosticIndex = 0; diagnosticIndex < diagnostics.Length; ++diagnosticIndex)
            {
                builder.AppendLine("// " + diagnostics[diagnosticIndex]);

                var analyzerType = analyzer.GetType();
                var descriptors = analyzer.SupportedDiagnostics;

                foreach (var descriptor in descriptors
                    .Where(descriptor => descriptor?.Id == diagnostics[diagnosticIndex].Id))
                {
                    var location = diagnostics[diagnosticIndex].Location;
                    if (location == Location.None)
                    {
                        builder.AppendFormat("GetGlobalResult({0}.{1})", analyzerType.Name, descriptor.Id);
                    }
                    else
                    {
                        Assert.IsTrue(
                            location.IsInSource,
                            $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[diagnosticIndex]}\r\n");

                        var position = diagnostics[diagnosticIndex].Location.GetLineSpan().StartLinePosition;

                        builder.AppendFormat("{0}({1}, {2}, {3}.{4})",
                            "GetResultAt",
                            position.Line + 1,
                            position.Character + 1,
                            analyzerType.Name,
                            descriptor.Id);
                    }

                    if (diagnosticIndex != diagnostics.Length - 1)
                    {
                        builder.Append(',');
                    }

                    builder.AppendLine();
                    break;
                }
            }

            return builder.ToString();
        }
    }
}
