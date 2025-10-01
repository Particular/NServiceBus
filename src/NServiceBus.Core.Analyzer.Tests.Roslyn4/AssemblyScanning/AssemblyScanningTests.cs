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
                       using System;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Installation;
                       using NServiceBus.Extensibility;
                       
                       [assembly:NServiceBus.Extensibility.SourceGeneratedAssemblyScanning(false)]

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

                       public class DownstreamSpecificType : IAmImportantNonCoreType { }

                       [NServiceBusExtensionPoint("RegisterImportant", true)]
                       public interface IAmImportantNonCoreType { }
                       
                       public class MySaga : Saga<MySagaData>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }
                       }
                       
                       public class MySagaData : ContainSagaData { }
                       """;

        var (output, _) = GetGeneratedOutput(source);

        // Assert.Multiple(() =>
        // {
        //     // Assert the g enerated registry contains expected types
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyEvent)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyCommand)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyMessage)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyHandler)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MySecondHandler)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.Installer)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.DownstreamSpecificType)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.IAmImportantNonCoreType)"));
        //     Assert.That(output, Does.Contain("typeof(UserCode.MyFeature)"));
        //
        //     // Assert the generated registry does not contain expected types
        //     Assert.That(output, Does.Not.Contain("typeof(UserCode.NotInteresting)"));
        // });

        Console.Write(output);
    }

    static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source, bool suppressGeneratedDiagnosticsErrors = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Source.cs");
        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // Add necessary references for the generated trigger
        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        var knownTypesGenerator = new KnownTypesGenerator();
        var scanningGenerator = new AssemblyScanningGenerator();
        var driver = CSharpGeneratorDriver.Create(knownTypesGenerator, scanningGenerator);

        // Run the generator first to provide the NsbHandlerAttribute
        var initialCompilation = CSharpCompilation.Create("initial", new[] { syntaxTree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        driver.RunGeneratorsAndUpdateCompilation(initialCompilation, out var outputCompilation, out var generateDiagnostics);

        // Now compile the final result with all generated sources
        Compile(outputCompilation.SyntaxTrees, references);

        if (!suppressGeneratedDiagnosticsErrors)
        {
            Assert.That(generateDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False, "Failed: " + generateDiagnostics.FirstOrDefault()?.GetMessage());
        }

        var generated = outputCompilation.SyntaxTrees
            .Where(st => st.FilePath != "Source.cs")
            .Select(st => $"""
                          {st.FilePath}
                          -------------
                          {st}
                          ========================================================================================
                          """);
        string combinedGeneration = string.Join("", generated);

        return (combinedGeneration, generateDiagnostics);
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