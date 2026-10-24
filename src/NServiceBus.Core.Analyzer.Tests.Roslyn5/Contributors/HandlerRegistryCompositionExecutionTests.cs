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

    [TestCase("cfg?.Handlers.AddAll();")]
    [TestCase("cfg.Handlers?.AddAll();")]
    [TestCase("cfg?.Handlers?.AddAll();")]
    public void ConditionalAccessCallsAreIntercepted(string statement)
    {
        var configuration = Configure("ConditionalAccess" + Math.Abs(statement.GetHashCode()), statement);

        Assert.That(Calls(configuration), Is.EqualTo("Apple.AddHandlers, Apple.AddSagas, Host.AddHandlers, Host.AddSagas, Zebra.AddHandlers, Zebra.AddSagas"));
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

    [Test]
    public void AssembliesWhoseSanitizedNamesCollideAreComposedSeparately()
    {
        var dotted = GeneratedCompilation.BuildLibrary("My.App", $"{Usings}\n\n{Body("One")}");
        var plain = GeneratedCompilation.BuildLibrary("MyApp", $"{Usings}\n\n{Body("Two")}");
        var underscored = GeneratedCompilation.BuildLibrary("My_App", $"{Usings}\n\n{Body("Three")}");

        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) => cfg.Handlers.AddAll();
                     }

                     {{Body("Host")}}
                     """;

        var output = GeneratedCompilation.Run(nameof(AssembliesWhoseSanitizedNamesCollideAreComposedSeparately), host, Generators, dotted, plain, underscored);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation, dotted, plain, underscored);
        var configuration = new EndpointConfiguration("Collisions");
        var test = assembly.GetType("Test")!;
        test.GetMethod("Configure")!.Invoke(Activator.CreateInstance(test), [configuration]);

        Assert.That(Calls(configuration), Is.EqualTo("Host.AddHandlers, Host.AddSagas, One.AddHandlers, One.AddSagas, Three.AddHandlers, Three.AddSagas, Two.AddHandlers, Two.AddSagas"));
    }

    [Test]
    public void InternalRegistryIsComposedWhenTheLibraryGrantsInternalsVisibleToTheHost()
    {
        var hostName = nameof(InternalRegistryIsComposedWhenTheLibraryGrantsInternalsVisibleToTheHost);
        var library = GeneratedCompilation.BuildLibrary("Granting", InternalLibrary("Granting", hostName));

        var configuration = ConfigureHost(hostName, "cfg.Handlers.AddAll();", library);

        Assert.That(Calls(configuration), Is.EqualTo("Granting.AddHandlers, Granting.AddSagas, Host.AddHandlers, Host.AddSagas"));
        Assert.That(Sagas(configuration), Is.EqualTo("Acme.Granting.GrantingPolicy, Acme.Host.HostPolicy"));
    }

    [Test]
    public void InternalRegistryIsSkippedWhenTheLibraryDoesNotGrantInternalsVisibleToTheHost()
    {
        var library = GeneratedCompilation.BuildLibrary("Closed", InternalLibrary("Closed", friendAssembly: null));

        var configuration = ConfigureHost(nameof(InternalRegistryIsSkippedWhenTheLibraryDoesNotGrantInternalsVisibleToTheHost), "cfg.Handlers.AddAll();", library);

        Assert.That(Calls(configuration), Is.EqualTo("Host.AddHandlers, Host.AddSagas"));
        Assert.That(Sagas(configuration), Is.EqualTo("Acme.Host.HostPolicy"));
    }

    [Test]
    public void InternalRegistryIsSkippedWhenTheLibraryGrantsInternalsVisibleToAnotherAssembly()
    {
        var library = GeneratedCompilation.BuildLibrary("Other", InternalLibrary("Other", "SomeoneElse"));

        var configuration = ConfigureHost(nameof(InternalRegistryIsSkippedWhenTheLibraryGrantsInternalsVisibleToAnotherAssembly), "cfg.Handlers.AddAll();", library);

        Assert.That(Calls(configuration), Is.EqualTo("Host.AddHandlers, Host.AddSagas"));
    }

    static readonly IIncrementalGenerator[] Generators = [new HandlerRegistryCompositionInterceptor(), new AddHandlerGenerator(), new AddSagaGenerator(), new AddHandlerAndSagasRegistrationGenerator()];

    static string InternalLibrary(string name, string friendAssembly) => $$"""
                                                                        {{Usings}}
                                                                        using System.Runtime.CompilerServices;
                                                                        {{(friendAssembly is null ? string.Empty : $"[assembly: InternalsVisibleTo(\"{friendAssembly}\")]")}}

                                                                        {{Body(name)}}

                                                                        [HandlerRegistryExtensions(EntryPointName = "{{name}}")]
                                                                        internal static partial class {{name}}Registrations
                                                                        {
                                                                        }
                                                                        """;

    static EndpointConfiguration Configure(string assemblyName, string statements)
    {
        var apple = GeneratedCompilation.BuildLibrary("Apple", $"{Usings}\n\n{Body("Apple")}");
        var zebra = GeneratedCompilation.BuildLibrary("Zebra", $"{Usings}\n\n{Body("Zebra")}");

        return ConfigureHost(assemblyName, statements, apple, zebra);
    }

    static EndpointConfiguration ConfigureHost(string assemblyName, string statements, params byte[][] libraries)
    {
        var host = $$"""
                     {{Usings}}

                     public class Test
                     {
                         public void Configure(EndpointConfiguration cfg) { {{statements}} }
                     }

                     {{Body("Host")}}
                     """;

        var output = GeneratedCompilation.Run(assemblyName, host, Generators, libraries);
        Assert.That(output.CompilationErrors, Is.Empty);

        var assembly = GeneratedCompilation.Load(output.Compilation, libraries);
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
