namespace NServiceBus.Core.Analyzer.Tests.AssemblyScanning;

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

public class UsingTestingFramework
{
    [Test]
    public async Task WithFramework()
    {
        var source = $$"""
                       using System;
                       using System.Threading;
                       using System.Threading.Tasks;
                       using NServiceBus;
                       using NServiceBus.Features;
                       using NServiceBus.Installation;
                       using NServiceBus.Extensibility;
                       
                       [assembly:NServiceBus.Extensibility.SourceGeneratedAssemblyScanning(true)]

                       namespace UserCode;
                       
                       public class Program
                       {
                           public void Main()
                           {
                               var cfg = new EndpointConfiguration("UserCode");
                               cfg.TurnOffAssemblyScanningAndUseSourceGenerationInstead(true);
                               cfg.TurnOffAssemblyScanningAndUseSourceGenerationInstead(false);
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
                       """;

        await GeneratorTestHarness.RunAsync<KnownTypesGenerator>(source);
    }
}

public static class GeneratorTestHarness
{
    public static async Task RunAsync<T>(string inputSource)
        where T : IIncrementalGenerator, new()
    {
        var test = new CSharpSourceGeneratorTest<T, DefaultVerifier>
        {
            TestState =
            {
                Sources = { inputSource },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", @"
is_global = true

# Required for interceptors
build_property.InterceptorsNamespaces = NServiceBus
")
                }
            }
        };

        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(IMessage).Assembly.Location));
        test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromFile(typeof(EndpointConfiguration).Assembly.Location));

        // Clear expected state so we can just dump results
        test.TestState.GeneratedSources.Clear();
        test.TestState.ExpectedDiagnostics.Clear();

        // Run the generators
        await test.RunAsync();

        // Dump out what happened
        Console.WriteLine("=== Generated Sources ===");
        foreach (var result in test.TestState.GeneratedSources)
        {
            var (filename, content) = result;
            Console.WriteLine($"Generator: {filename}");
            Console.WriteLine(content);
            Console.WriteLine("========================");
        }

        Console.WriteLine("=== Diagnostics ===");
        foreach (var diag in test.TestState.ExpectedDiagnostics)
        {
            Console.WriteLine(diag.ToString());
        }
    }
}