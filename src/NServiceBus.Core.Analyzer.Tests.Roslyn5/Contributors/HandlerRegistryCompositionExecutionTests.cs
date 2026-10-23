namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using System;
using System.Collections.Generic;
using System.Linq;
using Analyzer.Handlers;
using Microsoft.CodeAnalysis;
using Analyzer.Sagas;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Sagas;
using NServiceBus.Unicast;
using NUnit.Framework;

[TestFixture]
public class HandlerRegistryCompositionExecutionTests
{
    const string Usings = """
                          using System.Collections.Generic;
                          using System.Threading.Tasks;
                          using NServiceBus;
                          using NServiceBus.Configuration.AdvancedExtensibility;
                          """;

    static string Body(string name) => $$"""
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
    public void AddAllRunsRegistrationsOfTheLocalAndReferencedAssemblies()
    {
        var configuration = Configure(nameof(AddAllRunsRegistrationsOfTheLocalAndReferencedAssemblies), "cfg.Handlers.AddAll();");

        Assert.That(Calls(configuration), Is.EqualTo("Apple.AddHandlers, Apple.AddSagas, Host.AddHandlers, Host.AddSagas, Zebra.AddHandlers, Zebra.AddSagas"));
        Assert.That(HandledMessages(configuration), Is.EqualTo("Acme.Apple.AppleCommand, Acme.Apple.AppleStart, Acme.Host.HostCommand, Acme.Host.HostStart, Acme.Zebra.ZebraCommand, Acme.Zebra.ZebraStart"));
        Assert.That(Sagas(configuration), Is.EqualTo("Acme.Apple.ApplePolicy, Acme.Host.HostPolicy, Acme.Zebra.ZebraPolicy"));
    }

    [Test]
    public void AddAllHandlersRegistersNoSagas()
    {
        var configuration = Configure(nameof(AddAllHandlersRegistersNoSagas), "cfg.Handlers.AddAllHandlers();");

        Assert.That(Calls(configuration), Is.EqualTo("Apple.AddHandlers, Host.AddHandlers, Zebra.AddHandlers"));
        Assert.That(Sagas(configuration), Is.Empty);
    }

    [Test]
    public void AddAllSagasRegistersOnlySagas()
    {
        var configuration = Configure(nameof(AddAllSagasRegistersOnlySagas), "cfg.Handlers.AddAllSagas();");

        Assert.That(Calls(configuration), Is.EqualTo("Apple.AddSagas, Host.AddSagas, Zebra.AddSagas"));
        Assert.That(Sagas(configuration), Is.EqualTo("Acme.Apple.ApplePolicy, Acme.Host.HostPolicy, Acme.Zebra.ZebraPolicy"));
    }

    [Test]
    public void ComposedAddAllIsHarmlessAfterRegisteringTheAssemblyRegistryExplicitly()
    {
        var configuration = Configure(
            nameof(ComposedAddAllIsHarmlessAfterRegisteringTheAssemblyRegistryExplicitly),
            $"cfg.Handlers.{nameof(ComposedAddAllIsHarmlessAfterRegisteringTheAssemblyRegistryExplicitly)}Assembly.AddAll(); cfg.Handlers.AddAll();");

        Assert.That(HandledMessages(configuration), Is.EqualTo("Acme.Apple.AppleCommand, Acme.Apple.AppleStart, Acme.Host.HostCommand, Acme.Host.HostStart, Acme.Zebra.ZebraCommand, Acme.Zebra.ZebraStart"));
        Assert.That(Sagas(configuration), Is.EqualTo("Acme.Apple.ApplePolicy, Acme.Host.HostPolicy, Acme.Zebra.ZebraPolicy"));

        var handlers = configuration.GetSettings().Get<MessageHandlerRegistry>();
        var hostCommand = handlers.GetMessageTypes().Single(type => type.Name == "HostCommand");
        Assert.That(handlers.GetHandlersFor(hostCommand), Has.Count.EqualTo(1));
    }

    static readonly IIncrementalGenerator[] Generators = [new HandlerRegistryCompositionInterceptor(), new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()];

    static EndpointConfiguration Configure(string assemblyName, string statements)
    {
        var apple = GeneratedCompilation.BuildLibrary("Apple", $"{Usings}\n\n{Body("Apple")}");
        var zebra = GeneratedCompilation.BuildLibrary("Zebra", $"{Usings}\n\n{Body("Zebra")}");

        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) { {{statements}} }
                     }

                     {{Body("Host")}}
                     """;

        var output = GeneratedCompilation.Run(assemblyName, host, Generators, apple, zebra);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation, apple, zebra);
        var configuration = new EndpointConfiguration("Composition");
        var test = assembly.GetType("Test")!;
        test.GetMethod("Configure")!.Invoke(Activator.CreateInstance(test), [configuration]);
        return configuration;
    }

    static string Calls(EndpointConfiguration configuration) =>
        string.Join(", ", configuration.GetSettings().TryGet<List<string>>(out var calls) ? calls.Order(StringComparer.Ordinal) : []);

    static string HandledMessages(EndpointConfiguration configuration) =>
        string.Join(", ", configuration.GetSettings().Get<MessageHandlerRegistry>().GetMessageTypes().Select(type => type.FullName).Order(StringComparer.Ordinal));

    static string Sagas(EndpointConfiguration configuration) =>
        configuration.GetSettings().TryGet<SagaMetadataCollection>(out var sagas)
            ? string.Join(", ", sagas.Select(saga => saga.SagaType.FullName).Order(StringComparer.Ordinal))
            : string.Empty;
}
