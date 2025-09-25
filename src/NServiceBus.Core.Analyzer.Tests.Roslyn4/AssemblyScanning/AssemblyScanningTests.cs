namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

[TestFixture]
public class AssemblyScanningTests
{
    [Test]
    public void Basic()
    {
        var source = $$"""
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Installation;

                       namespace UserCode;

                       public class MyEvent : IEvent {}
                       public class MyCommand : ICommand {}
                       public class MyMessage : IMessage {}
                       public class MyFeature : Feature
                       {
                           protected override void Setup(FeatureConfigurationContext context) { }
                       }
                       public class MyHandler : IHandleMessages<MyEvent>
                       {
                           public Task Handle(MyEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class MySecondHandler : IHandleMessages<MyCommand>
                       {
                           public Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class Installer : INeedToInstallSomething
                       {
                           public Task Install(string identity, CancellationToken cancellationToken) => Task.CompletedTask;
                       }
                       public class NotInteresting { }
                       """;

        var (output, diagnostics) = GetGeneratedOutput(source);

        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine(diagnostic);
        }

        Console.WriteLine("----------");
        Console.WriteLine(output);
    }

    static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source, bool suppressGeneratedDiagnosticsErrors = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var compilation = Compile(new[]
        {
            syntaxTree
        }, references);

        var generator = new KnownTypesGenerator();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);

        // add necessary references for the generated trigger
        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));
        Compile(outputCompilation.SyntaxTrees, references);

        if (!suppressGeneratedDiagnosticsErrors)
        {
            Assert.That(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False, "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
        }

        return (outputCompilation.SyntaxTrees.Last().ToString(), generateDiagnostics);
    }

    static CSharpCompilation Compile(IEnumerable<SyntaxTree> syntaxTrees, IEnumerable<MetadataReference> references)
    {
        var compilation = CSharpCompilation.Create("result", syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Verify the code compiled:
        var compilationErrors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.That(compilationErrors, Is.Empty, compilationErrors.FirstOrDefault()?.GetMessage());

        return compilation;
    }

}