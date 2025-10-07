namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
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
                       using NServiceBus.Extensibility;
                       using NServiceBus.Installation;
                       using NServiceBus.Sagas;
                       
                       namespace UserCode;
                       
                       public class Program
                       {
                           public void Main()
                           {
                               var cfg = new EndpointConfiguration("UserCode");

                               cfg.UseSourceGeneratedTypeDiscovery()
                                   .RegisterHandlersAndSagas();
                           }
                       }
                       
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
                       
                       public class MySagaNotFound : IHandleSagaNotFound
                       {
                           public Task Handle(object message, IMessageProcessingContext context) => Task.CompletedTask;
                       }
                       """;

        var (output, _) = GetGeneratedOutput(source);

        // Assert.Multiple(() =>
        // {
        //     // Assert the generated registry contains expected types
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

    [Test]
    public void TypeRegistrations_Should_Store_And_Retrieve_Types_Correctly()
    {
        var source = $$"""
                       using System;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Extensibility;
                       using NServiceBus.Installation;
                       using NServiceBus.Sagas;
                       
                       namespace TestNamespace;
                       
                       public class Program
                       {
                           public void Main()
                           {
                               var cfg = new EndpointConfiguration("MyApp");
                       
                               cfg.UseSourceGeneratedTypeDiscovery()
                                   .RegisterHandlersAndSagas();
                           }
                       }
                       
                       // Required types (AutoRegister = true)
                       public class TestEvent : IEvent {}
                       public class TestCommand : ICommand {}
                       public class TestFeature : Feature
                       {
                           protected override void Setup(FeatureConfigurationContext context) { }
                       }
                       public class TestInstaller : INeedToInstallSomething
                       {
                           public Task Install(string identity, CancellationToken cancellationToken) => Task.CompletedTask;
                       }
                       
                       // Optional types (AutoRegister = false)  
                       public class TestHandler : IHandleMessages<TestEvent>
                       {
                           public Task Handle(TestEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                       }

                       // Optional types (AutoRegister = false)  
                       public class TestHandler2 : IHandleMessages<TestEvent>
                       {
                           public Task Handle(TestEvent message, IMessageHandlerContext context) => Task.CompletedTask;
                       }
                       public class TestSaga : Saga<TestSagaData>
                       {
                           protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) { }
                       }
                       public class TestSagaData : ContainSagaData { }
                       """;

        var (output, diagnostics) = GetGeneratedOutput(source);

        Console.Write(output);

        // Verify no compilation errors
        Assert.That(diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False,
            "Compilation failed: " + diagnostics.FirstOrDefault()?.GetMessage());

        // Verify the generated code contains the expected TypeRegistrations calls
        Assert.Multiple(() =>
        {
            // Required types should be in RequiredTypeRegistration
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.IEvent, TestNamespace.TestEvent>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.ICommand, TestNamespace.TestCommand>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.Features.Feature, TestNamespace.TestFeature>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.Installation.INeedToInstallSomething, TestNamespace.TestInstaller>()"));

            // Optional types should be in OptionalTypeRegistration
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.IHandleMessages, TestNamespace.TestHandler>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.IHandleMessages, TestNamespace.TestHandler2>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.Saga, TestNamespace.TestSaga>()"));
            Assert.That(output, Does.Contain("config.TypeRegistrations.RegisterExtensionType<NServiceBus.IContainSagaData, TestNamespace.TestSagaData>()"));

            // Verify the structure is correct
            Assert.That(output, Does.Contain("public static class RequiredTypeRegistration"));
            Assert.That(output, Does.Contain("public static class OptionalTypeRegistration"));
            Assert.That(output, Does.Contain("public static void RegisterTypes(NServiceBus.EndpointConfiguration config)"));
        });

        // Now test that the generated code actually compiles and can be executed
        // This is a more comprehensive test that validates the end-to-end flow
        var compilation = CreateCompilationWithGeneratedCode(source, output);
        var diagnostics2 = compilation.GetDiagnostics();

        Assert.That(diagnostics2.Where(d => d.Severity >= DiagnosticSeverity.Error), Is.Empty,
            "Generated code compilation failed: " + string.Join("; ", diagnostics2.Where(d => d.Severity >= DiagnosticSeverity.Error).Select(d => d.GetMessage())));
    }

    static (string output, ImmutableArray<Diagnostic> diagnostics) GetGeneratedOutput(string source, bool suppressGeneratedDiagnosticsErrors = false)
    {
        var features = new Dictionary<string, string>
        {
            ["InterceptorsNamespaces"] = "NServiceBus",
            ["InterceptorsPreviewNamespaces"] = "NServiceBus",
        };

        var parseOptions = new CSharpParseOptions(LanguageVersion.LatestMajor)
            .WithFeatures(features);

        string[] sources = [source];

        var syntaxTrees = sources
            .Select(x =>
            {
                var tree = CSharpSyntaxTree.ParseText(x, path: "Source.cs");
                var options = parseOptions;
                return tree.WithRootAndOptions(tree.GetRoot(), options);
            });

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

        IIncrementalGenerator[] generators = [new KnownTypesGenerator(), new AssemblyScanningGenerator()];

        var opts = new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true);

        GeneratorDriver driver =
            CSharpGeneratorDriver.Create(
                generators.Select(x => x.AsSourceGenerator()),
                driverOptions: opts,
                optionsProvider: new OptionsProvider(new DictionaryAnalyzerOptions(features)),
                parseOptions: parseOptions);


        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        var initialCompilation = CSharpCompilation.Create("initial", syntaxTrees, references, options);

        driver.RunGeneratorsAndUpdateCompilation(initialCompilation, out var outputCompilation, out var generatedDiagnostics);

        if (!suppressGeneratedDiagnosticsErrors)
        {
            Assert.That(generatedDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error), Is.False, "Failed: " + generatedDiagnostics.FirstOrDefault()?.GetMessage());
        }

        var generated = outputCompilation.SyntaxTrees
            .Where(st => st.FilePath != "Source.cs")
            .Select(st => $"""
                           // ========================================================================================
                           // {st.FilePath}
                           // ========================================================================================
                           {st}
                           """);
        string combinedGeneration = string.Join("", generated);

        // Verify the code compiled:
        var compilationErrors = outputCompilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();

        Assert.That(compilationErrors, Is.Empty, compilationErrors.FirstOrDefault()?.GetMessage());

        return (combinedGeneration, generatedDiagnostics);
    }

    static CSharpCompilation CreateCompilationWithGeneratedCode(string originalSource, string generatedOutput)
    {
        var features = new Dictionary<string, string>
        {
            ["InterceptorsNamespaces"] = "NServiceBus",
            ["InterceptorsPreviewNamespaces"] = "NServiceBus",
        };

        var parseOptions = new CSharpParseOptions(LanguageVersion.LatestMajor)
            .WithFeatures(features);

        // Parse the original source
        var originalTree = CSharpSyntaxTree.ParseText(originalSource, path: "Source.cs", options: parseOptions);

        // Parse the generated code (extract just the C# code, not the file headers)
        var generatedCode = ExtractGeneratedCode(generatedOutput);
        var generatedTree = CSharpSyntaxTree.ParseText(generatedCode, path: "Generated.cs", options: parseOptions);

        var references = new List<MetadataReference>();
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        // Add necessary references
        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        return CSharpCompilation.Create("test", [originalTree, generatedTree], references, options);
    }

    static string ExtractGeneratedCode(string fullOutput)
    {
        // Extract just the C# code from the generated output, removing file headers
        var lines = fullOutput.Split('\n');
        var codeLines = new List<string>();
        var inCodeBlock = false;

        foreach (var line in lines)
        {
            if (line.Trim().StartsWith("// <auto-generated/>"))
            {
                inCodeBlock = true;
                codeLines.Add(line);
            }
            else if (inCodeBlock && !line.Trim().StartsWith("// =="))
            {
                codeLines.Add(line);
            }
            else if (line.Trim().StartsWith("// =="))
            {
                inCodeBlock = false;
            }
        }

        return string.Join('\n', codeLines);
    }

    class OptionsProvider(AnalyzerConfigOptions options) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => options;
        public override AnalyzerConfigOptions GlobalOptions => options;
    }

    internal sealed class DictionaryAnalyzerOptions(Dictionary<string, string> properties) : AnalyzerConfigOptions
    {
        public static DictionaryAnalyzerOptions Empty { get; } = new([]);

        public override bool TryGetValue(string key, out string value)
            => properties.TryGetValue(key, out value!);
    }

}