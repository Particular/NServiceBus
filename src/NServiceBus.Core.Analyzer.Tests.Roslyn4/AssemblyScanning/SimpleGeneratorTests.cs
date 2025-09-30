namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

/// <summary>
/// Simplified generator tests that verify each type category individually.
/// </summary>
[TestFixture]
public class SimpleGeneratorTests
{
    [Test]
    public void Generator_Should_Find_Handler_Types()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     namespace TestApp;
                     
                     public class MyMessage : ICommand { }
                     
                     public class MyHandler : IHandleMessages<MyMessage>
                     {
                         public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     """;

        var generatedCode = GetGeneratedCode(source);
        Console.WriteLine("Generated code length: " + generatedCode.Length);
        Console.WriteLine("Generated code:");
        Console.WriteLine(generatedCode);
        
        var types = GetDiscoveredTypes(source);

        Assert.That(types, Is.Not.Empty, "Generator should discover at least one type");
        Assert.That(types, Does.Contain("TestApp.MyHandler"), "Should find handler type");
        
        Console.WriteLine("Discovered types:");
        foreach (var type in types)
        {
            Console.WriteLine($"  - {type}");
        }
    }

    [Test]
    public void Generator_Should_Find_Message_Types()
    {
        var source = """
                     using NServiceBus;

                     namespace TestApp;
                     
                     public class MyEvent : IEvent { }
                     public class MyCommand : ICommand { }
                     public class MyMessage : IMessage { }
                     """;

        var generatedCode = GetGeneratedCode(source);
        Console.WriteLine("[MESSAGE TEST] Generated code length: " + generatedCode.Length);
        
        var types = GetDiscoveredTypes(source);

        Assert.That(types, Is.Not.Empty, "Generator should discover message types");
        Assert.Multiple(() =>
        {
            Assert.That(types, Does.Contain("TestApp.MyEvent"), "Should find event");
            Assert.That(types, Does.Contain("TestApp.MyCommand"), "Should find command");
            Assert.That(types, Does.Contain("TestApp.MyMessage"), "Should find message");
        });
        
        Console.WriteLine("Discovered types:");
        foreach (var type in types)
        {
            Console.WriteLine($"  - {type}");
        }
    }

    [Test]
    public void Generator_Should_Find_Infrastructure_Types()
    {
        var source = """
                     using System.Threading;
                     using System.Threading.Tasks;
                     using NServiceBus;
                     using NServiceBus.Features;
                     using NServiceBus.Installation;

                     namespace TestApp;
                     
                     public class MyFeature : Feature
                     {
                         protected internal override void Setup(FeatureConfigurationContext context) { }
                     }
                     
                     public class MyInstaller : INeedToInstallSomething
                     {
                         public Task Install(string identity, CancellationToken cancellationToken) => Task.CompletedTask;
                     }
                     
                     public class MyInitializer : INeedInitialization
                     {
                         public void Customize(EndpointConfiguration configuration) { }
                     }
                     """;

        var types = GetDiscoveredTypes(source);

        Assert.That(types, Is.Not.Empty, "Generator should discover infrastructure types");
        Assert.Multiple(() =>
        {
            Assert.That(types, Does.Contain("TestApp.MyFeature"), "Should find feature");
            Assert.That(types, Does.Contain("TestApp.MyInstaller"), "Should find installer");
            Assert.That(types, Does.Contain("TestApp.MyInitializer"), "Should find initializer");
        });
        
        Console.WriteLine("Discovered types:");
        foreach (var type in types)
        {
            Console.WriteLine($"  - {type}");
        }
    }

    [Test]
    public void Generator_Should_Produce_RegisterTypes_Method()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     namespace TestApp;
                     
                     public class MyMessage : ICommand { }
                     
                     public class MyHandler : IHandleMessages<MyMessage>
                     {
                         public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     """;

        var generatedCode = GetGeneratedCode(source);

        Assert.That(generatedCode, Is.Not.Null.And.Not.Empty, "Generator should produce code");
        
        Assert.Multiple(() =>
        {
            Assert.That(generatedCode, Does.Contain("namespace NServiceBus.Generated"), 
                "Should generate in NServiceBus.Generated namespace");
            Assert.That(generatedCode, Does.Contain("class TypeRegistration"), 
                "Should generate TypeRegistration class");
            Assert.That(generatedCode, Does.Contain("void RegisterTypes"), 
                "Should generate RegisterTypes method");
        });
        
        Console.WriteLine("Generated code:");
        Console.WriteLine(generatedCode);
    }

    [Test]
    public void Generator_Should_Produce_Registration_Calls()
    {
        var source = """
                     using System.Threading.Tasks;
                     using NServiceBus;

                     namespace TestApp;
                     
                     public class MyCommand : ICommand { }
                     public class MyEvent : IEvent { }
                     
                     public class MyHandler : IHandleMessages<MyCommand>
                     {
                         public Task Handle(MyCommand message, IMessageHandlerContext context) => Task.CompletedTask;
                     }
                     """;

        var generatedCode = GetGeneratedCode(source);

        Assert.That(generatedCode, Is.Not.Null.And.Not.Empty, "Generator should produce code");
        
        // Check for registration method calls
        Assert.Multiple(() =>
        {
            Assert.That(generatedCode, Does.Contain("RegisterHandler"), 
                "Should contain RegisterHandler call");
            Assert.That(generatedCode, Does.Contain("RegisterCommand"), 
                "Should contain RegisterCommand call");
            Assert.That(generatedCode, Does.Contain("RegisterEvent"), 
                "Should contain RegisterEvent call");
            Assert.That(generatedCode, Does.Contain("TestApp.MyHandler"), 
                "Should reference handler type");
        });
        
        Console.WriteLine("Generated code:");
        Console.WriteLine(generatedCode);
    }

    /// <summary>
    /// Gets the list of types discovered by the generator (from debug output or internal state).
    /// This is a simplified approach that looks at the generated code to extract type names.
    /// </summary>
    static List<string> GetDiscoveredTypes(string source)
    {
        var generatedCode = GetGeneratedCode(source);
        
        if (string.IsNullOrWhiteSpace(generatedCode))
        {
            return [];
        }

        // Extract type names from registration calls like "config.RegisterHandler<TypeName>()"
        List<string> types = [];
        var lines = generatedCode.Split('\n');
        
        foreach (var line in lines)
        {
            if (line.Contains("config.Register"))
            {
                var start = line.IndexOf('<');
                var end = line.IndexOf('>');
                if (start > 0 && end > start)
                {
                    var typeName = line.Substring(start + 1, end - start - 1);
                    // Handle saga registrations like "RegisterSaga<TSaga, TSagaData>"
                    if (typeName.Contains(','))
                    {
                        typeName = typeName.Split(',')[0].Trim();
                    }
                    if (!types.Contains(typeName))
                    {
                        types.Add(typeName);
                    }
                }
            }
        }
        
        return types;
    }

    /// <summary>
    /// Runs the generator and returns the generated code.
    /// </summary>
    static string GetGeneratedCode(string source)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);
            List<MetadataReference> references = [];
            
            // Add essential references
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }

            // Ensure NServiceBus.Core reference
            references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
            references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Check if IHandleMessages can be found
            var handleMessagesType = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
            Console.WriteLine($"IHandleMessages`1 found: {handleMessagesType != null}");
            var messageType = compilation.GetTypeByMetadataName("NServiceBus.IMessage");
            Console.WriteLine($"IMessage found: {messageType != null}");
            var eventType = compilation.GetTypeByMetadataName("NServiceBus.IEvent");
            Console.WriteLine($"IEvent found: {eventType != null}");
            
            // Check if types from source are found
            var myHandlerType = compilation.GetTypeByMetadataName("TestApp.MyHandler");
            Console.WriteLine($"TestApp.MyHandler found: {myHandlerType != null}");
            var myMessageType = compilation.GetTypeByMetadataName("TestApp.MyMessage");
            Console.WriteLine($"TestApp.MyMessage found: {myMessageType != null}");
            
            // Check if MyHandler implements IHandleMessages
            if (myHandlerType != null && handleMessagesType != null)
            {
                var implementsHandler = myHandlerType.AllInterfaces.Any(i => 
                    i.OriginalDefinition.Equals(handleMessagesType, SymbolEqualityComparer.Default));
                Console.WriteLine($"MyHandler implements IHandleMessages: {implementsHandler}");
            }

            // Check for compilation errors BEFORE running generator
            var compilationDiagnostics = compilation.GetDiagnostics();
            var compilationErrors = compilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (compilationErrors.Any())
            {
                Console.WriteLine("Compilation errors before generator:");
                foreach (var error in compilationErrors)
                {
                    Console.WriteLine($"  {error.GetMessage()}");
                }
            }

            var generator = new KnownTypesGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);

            driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Check for ALL generator diagnostics
            Console.WriteLine($"Generator diagnostics count: {diagnostics.Length}");
            foreach (var diag in diagnostics)
            {
                Console.WriteLine($"  [{diag.Severity}] {diag.Id}: {diag.GetMessage()}");
            }
            
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            if (errors.Any())
            {
                Console.WriteLine("Generator errors:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  {error.GetMessage()}");
                }
            }

            Console.WriteLine($"Total syntax trees after generation: {outputCompilation.SyntaxTrees.Count()}");
            foreach (var tree in outputCompilation.SyntaxTrees)
            {
                var treeString = tree.ToString();
                if (treeString.Contains("Generated") || treeString.Contains("TypeRegistration"))
                {
                    Console.WriteLine($"Found generated tree with length: {treeString.Length}");
                }
            }

            // Find the generated TypeRegistration file
            var generated = outputCompilation.SyntaxTrees
                .Select(st => st.ToString())
                .FirstOrDefault(text => text.Contains("namespace NServiceBus.Generated") && text.Contains("TypeRegistration"));

            return generated ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running generator: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return string.Empty;
        }
    }
}

