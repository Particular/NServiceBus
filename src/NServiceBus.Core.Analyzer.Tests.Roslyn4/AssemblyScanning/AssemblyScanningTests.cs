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
    [Ignore("moving to more discrete tests in other files")]
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
                       public class MyInitializer : INeedInitialization
                       {
                           public void Customize(EndpointConfiguration configuration) { }
                       }
                       public class NotInteresting { }

                       public class DownstreamSpecificType : IAmImportantNonCoreType { }

                       [NServiceBusExtensionPoint("RegisterImportant")]
                       public interface IAmImportantNonCoreType { }
                       """;

        var (output, _) = GetGeneratedOutput(source);
        
        Console.Write(output);

        Assert.Multiple(() =>
        {
            // Assert the generated code contains the RegisterTypes method
            Assert.That(output, Does.Contain("public static void RegisterTypes(NServiceBus.EndpointConfiguration config)"));

            // Assert the generated code contains expected registration calls for handlers
            Assert.That(output, Does.Contain("config.RegisterHandler<UserCode.MyHandler>()"));
            Assert.That(output, Does.Contain("config.RegisterHandler<UserCode.MySecondHandler>()"));

            // TEMPORARILY COMMENTED FOR DEBUGGING
            // Assert the generated code contains expected registration calls for messages
            //Assert.That(output, Does.Contain("config.RegisterEvent<UserCode.MyEvent>()"));
            //Assert.That(output, Does.Contain("config.RegisterCommand<UserCode.MyCommand>()"));
            //Assert.That(output, Does.Contain("config.RegisterMessage<UserCode.MyMessage>()"));

            // Assert the generated code contains expected registration calls for infrastructure types
            //Assert.That(output, Does.Contain("config.RegisterInstaller<UserCode.Installer>()"));
            //Assert.That(output, Does.Contain("config.RegisterFeature<UserCode.MyFeature>()"));
            //Assert.That(output, Does.Contain("config.RegisterInitializer<UserCode.MyInitializer>()"));

            // TODO: Custom extension point support not yet implemented
            // Assert.That(output, Does.Contain("UserCode.DownstreamSpecificType"));
            // Assert.That(output, Does.Contain("UserCode.IAmImportantNonCoreType"));

            // Assert the generated code does not contain uninteresting types
            Assert.That(output, Does.Not.Contain("NotInteresting"));
        });

       
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

        // Add necessary references for the generated trigger
        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        var generator = new KnownTypesGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

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
            .Select(st => st.ToString())
            .FirstOrDefault(text => text.Contains("namespace NServiceBus.Generated") && text.Contains("TypeRegistration"))
            ?? string.Empty;

        return (generated, generateDiagnostics);
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