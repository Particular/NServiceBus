namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System.Linq;
using Analyzer.Handlers;
using Analyzer.Sagas;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class HandlerRegistryCompositionInterceptorTests
{
    const string InterceptorHintName = "InterceptionsOfHandlerRegistryAddAllMethods.g.cs";

    const string Usings = """
                          using System.Collections.Generic;
                          using System.Threading.Tasks;
                          using NServiceBus;
                          using NServiceBus.Configuration.AdvancedExtensibility;
                          """;

    static string Library(string name) => $"{Usings}\n\n{LibraryBody(name)}";

    static string LibraryBody(string name) => $$"""
                                           namespace Acme.{{name}}
                                           {
                                               [Handler]
                                               public class {{name}}Handler : IHandleMessages<{{name}}Command>
                                               {
                                                   public Task Handle({{name}}Command message, IMessageHandlerContext context) => Task.CompletedTask;
                                               }

                                               [Saga]
                                               public class {{name}}Policy : Saga<{{name}}PolicyData>, IAmStartedByMessages<{{name}}Start>
                                               {
                                                   protected override void ConfigureHowToFindSaga(SagaPropertyMapper<{{name}}PolicyData> mapper) =>
                                                       mapper.MapSaga(s => s.CorrelationId).ToMessage<{{name}}Start>(m => m.CorrelationId);

                                                   public Task Handle({{name}}Start message, IMessageHandlerContext context) => Task.CompletedTask;
                                               }

                                               public class {{name}}PolicyData : ContainSagaData
                                               {
                                                   public string CorrelationId { get; set; }
                                               }

                                               public class {{name}}Command : ICommand { }

                                               public class {{name}}Start : ICommand
                                               {
                                                   public string CorrelationId { get; set; }
                                               }

                                               [HandlerRegistryContributor]
                                               internal sealed class {{name}}Contributor : IHandlerRegistryContributor<{{name}}Contributor>
                                               {
                                                   public static {{name}}Contributor Instance { get; } = new();

                                                   public void AddHandlers(EndpointConfiguration configuration) => configuration.GetSettings().GetOrCreate<List<string>>().Add("{{name}}.AddHandlers");

                                                   public void AddSagas(EndpointConfiguration configuration) => configuration.GetSettings().GetOrCreate<List<string>>().Add("{{name}}.AddSagas");
                                               }
                                           }
                                           """;

    [Test]
    public void ReferencedAssembliesAreComposed()
    {
        var library = GeneratedCompilation.BuildLibrary("AcmeLibrary", Library("Library"));

        var host = """
                   using NServiceBus;

                   public class Test
                   {
                       public void Configure(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                   }
                   """;

        SourceGeneratorTest.ForIncrementalGenerator<HandlerRegistryCompositionInterceptor>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .AddReferences(MetadataReference.CreateFromImage(library))
            .WithSource(host, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void LocalAssemblyIsComposedInAssemblyNameOrderWithReferencedAssemblies()
    {
        var zebra = GeneratedCompilation.BuildLibrary("Zebra", Library("Zebra"));
        var apple = GeneratedCompilation.BuildLibrary("Apple", Library("Apple"));

        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg)
                         {
                             cfg.Handlers.AddAll();
                             cfg.Handlers.AddAllHandlers();
                             cfg.Handlers.AddAllSagas();
                         }
                     }

                     {{LibraryBody("Host")}}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<HandlerRegistryCompositionInterceptor>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .AddReferences(MetadataReference.CreateFromImage(zebra), MetadataReference.CreateFromImage(apple))
            .WithSource(host, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void LocalAssemblyThatOptsOutIsNotComposed()
    {
        var apple = GeneratedCompilation.BuildLibrary("Apple", Library("Apple"));
        var optedOut = GeneratedCompilation.BuildLibrary("OptedOut", Library("OptedOut"), GeneratedCompilation.OptOutOfComposition);

        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                     }

                     {{LibraryBody("Host")}}
                     """;

        SourceGeneratorTest.ForIncrementalGenerator<HandlerRegistryCompositionInterceptor>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithProperty("build_property.NServiceBusHandlerRegistryComposition", "false")
            .AddReferences(MetadataReference.CreateFromImage(apple), MetadataReference.CreateFromImage(optedOut))
            .WithSource(host, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void LocalContributorOnlyAssemblyIsComposed()
    {
        var host = """
                   using NServiceBus;

                   public class Test
                   {
                       public void Configure(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                   }

                   [HandlerRegistryContributor]
                   internal sealed class WorkflowContributor : IHandlerRegistryContributor<WorkflowContributor>
                   {
                       public static WorkflowContributor Instance { get; } = new();

                       public void AddHandlers(EndpointConfiguration configuration) { }
                       public void AddSagas(EndpointConfiguration configuration) { }
                   }
                   """;

        SourceGeneratorTest.ForIncrementalGenerator<HandlerRegistryCompositionInterceptor>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .WithSource(host, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void MultipleCallSitesOfTheSameKindShareOneInterceptor()
    {
        var library = GeneratedCompilation.BuildLibrary("AcmeLibrary", Library("Library"));

        var host = """
                   using NServiceBus;

                   public class Test
                   {
                       public void First(EndpointConfiguration cfg) => cfg.Handlers.AddAll();

                       public void Second(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                   }
                   """;

        SourceGeneratorTest.ForIncrementalGenerator<HandlerRegistryCompositionInterceptor>()
            .WithIncrementalGenerator<AddHandlerAndSagasRegistrationGenerator>()
            .AddReferences(MetadataReference.CreateFromImage(library))
            .WithSource(host, "test.cs")
            .Run()
            .Approve()
            .AssertRunsAreEqual();
    }

    [Test]
    public void NoInvocationProducesNoInterceptor()
    {
        var library = GeneratedCompilation.BuildLibrary("AcmeLibrary", Library("Library"));

        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) { }
                     }

                     {{LibraryBody("Host")}}
                     """;

        var output = GeneratedCompilation.Run(nameof(NoInvocationProducesNoInterceptor), host, [new HandlerRegistryCompositionInterceptor(), new AddHandlerAndSagasRegistrationGenerator()], library);

        Assert.That(output.Sources.Select(source => source.HintName), Does.Not.Contain(InterceptorHintName));
    }

    [Test]
    public void CopycatAndNamespaceRegistryMethodsAreIgnored()
    {
        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg, Other.HandlerRegistry copycat)
                         {
                             copycat.AddAll();
                             cfg.Handlers.CopycatAndNamespaceRegistryMethodsAreIgnoredAssembly.AddAll();
                             cfg.Handlers.CopycatAndNamespaceRegistryMethodsAreIgnoredAssembly.AddAllHandlers();
                         }
                     }

                     namespace Other
                     {
                         public class HandlerRegistry
                         {
                             public void AddAll() { }
                         }
                     }

                     {{LibraryBody("Host")}}
                     """;

        var output = GeneratedCompilation.Run(nameof(CopycatAndNamespaceRegistryMethodsAreIgnored), host, [new HandlerRegistryCompositionInterceptor(), new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()]);

        Assert.That(output.Sources.Select(source => source.HintName), Does.Not.Contain(InterceptorHintName));
    }

    [Test]
    public void AssembliesWithoutTheGeneratedAttributeAreSkipped()
    {
        var plain = GeneratedCompilation.EmitImage(GeneratedCompilation.Run("PlainLibrary", Library("Plain"), []).Compilation);

        var host = """
                   using NServiceBus;

                   public class Test
                   {
                       public void Configure(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                   }
                   """;

        var output = GeneratedCompilation.Run(nameof(AssembliesWithoutTheGeneratedAttributeAreSkipped), host, [new HandlerRegistryCompositionInterceptor(), new AddHandlerAndSagasRegistrationGenerator()], plain);

        Assert.That(output.CompilationErrors, Is.Empty);
        var interceptor = output.SourceOf(InterceptorHintName);
        Assert.That(interceptor, Does.Contain("ThrowIfNull(registry);").And.Not.Contain("HandlerRegistryEntry"));
    }
}
