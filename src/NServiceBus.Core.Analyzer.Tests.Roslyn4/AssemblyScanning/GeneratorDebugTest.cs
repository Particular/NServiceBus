namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

/// <summary>
/// Debug test to isolate the generator issue
/// </summary>
[TestFixture]
public class GeneratorDebugTest
{
    [Test]
    public void Debug_Generator_With_Handler()
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

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        List<MetadataReference> references = [];
        
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (!assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        references.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Get ALL the marker types from the generator's list
        var markerNames = new[]
        {
            "NServiceBus.IEvent",
            "NServiceBus.ICommand",
            "NServiceBus.IMessage",
            "NServiceBus.IHandleMessages`1",
            "NServiceBus.Saga`1",
            "NServiceBus.Installation.INeedToInstallSomething",
            "NServiceBus.Features.Feature",
            "NServiceBus.INeedInitialization"
        };
        
        Console.WriteLine("Marker types found in compilation:");
        foreach (var markerName in markerNames)
        {
            var marker = compilation.GetTypeByMetadataName(markerName);
            Console.WriteLine($"  {markerName}: {marker != null}");
        }
        
        var handleMessagesMarker = compilation.GetTypeByMetadataName("NServiceBus.IHandleMessages`1");
        var commandMarker = compilation.GetTypeByMetadataName("NServiceBus.ICommand");
        
        Assert.That(handleMessagesMarker, Is.Not.Null, "Should find IHandleMessages marker");
        Assert.That(commandMarker, Is.Not.Null, "Should find ICommand marker");
        
        // Get our types
        var myHandlerType = compilation.GetTypeByMetadataName("TestApp.MyHandler");
        var myMessageType = compilation.GetTypeByMetadataName("TestApp.MyMessage");
        
        Assert.That(myHandlerType, Is.Not.Null, "Should find MyHandler");
        Assert.That(myMessageType, Is.Not.Null, "Should find MyMessage");
        
        // Check if MyMessage implements ICommand
        var messageImplementsCommand = myMessageType!.AllInterfaces.Any(i => 
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, commandMarker) ||
            SymbolEqualityComparer.Default.Equals(i, commandMarker));
        
        Console.WriteLine($"MyMessage implements ICommand: {messageImplementsCommand}");
        Assert.That(messageImplementsCommand, Is.True, "MyMessage should implement ICommand");
        
        // Check if MyHandler implements IHandleMessages<MyMessage>
        Console.WriteLine($"MyHandler interfaces count: {myHandlerType!.AllInterfaces.Length}");
        foreach (var iface in myHandlerType.AllInterfaces)
        {
            Console.WriteLine($"  Interface: {iface.ToDisplayString()}");
            Console.WriteLine($"  OriginalDefinition: {iface.OriginalDefinition.ToDisplayString()}");
            
            var matches1 = SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handleMessagesMarker);
            var matches2 = SymbolEqualityComparer.Default.Equals(iface, handleMessagesMarker);
            
            Console.WriteLine($"  OriginalDefinition == marker: {matches1}");
            Console.WriteLine($"  Interface == marker: {matches2}");
        }
        
        var handlerImplementsIHandleMessages = myHandlerType.AllInterfaces.Any(i => 
            SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, handleMessagesMarker));
        
        Console.WriteLine($"MyHandler implements IHandleMessages: {handlerImplementsIHandleMessages}");
        Assert.That(handlerImplementsIHandleMessages, Is.True, "MyHandler should implement IHandleMessages");
    }
}

