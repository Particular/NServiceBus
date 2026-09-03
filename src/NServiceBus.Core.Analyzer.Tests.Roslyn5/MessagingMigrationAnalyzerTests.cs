#nullable enable

#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class MessagingMigrationAnalyzerTests : AnalyzerTestFixture<MessagingMigrationAnalyzer>
{
    protected override void ConfigureFixtureTests(AnalyzerTest test)
    {
        base.ConfigureFixtureTests(test);
        test.WithProperty("build_property.PublishTrimmed", "true");
    }

    static AnalyzerTest MigrationTest(string source) =>
        AnalyzerTest.ForAnalyzer<MessagingMigrationAnalyzer>()
            .WithSource(source);

    static readonly MetadataReference TestingFakesReference =
        MetadataReference.CreateFromFile(typeof(NServiceBus.Testing.TestableMessageSession).Assembly.Location);

    static AnalyzerTest MigrationAuditTest(string source) =>
        MigrationTest(source).WithProperty("build_property.PublishTrimmed", "true");

    static AnalyzerTest FakeMigrationTest(string source) =>
        MigrationAuditTest(source).AddReferences(TestingFakesReference);

    static async Task AssertEditorConfigSeverity(
        string source,
        string diagnosticId,
        ReportDiagnostic severity,
        bool automaticActivation,
        params string[] expectedDiagnosticIds)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
        var analyzerDiagnostics = await GetEditorConfigDiagnostics(
            [syntaxTree],
            syntaxTree,
            diagnosticId,
            severity,
            automaticActivation);

        NUnit.Framework.Assert.That(analyzerDiagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo(expectedDiagnosticIds));
    }

    static async Task<ImmutableArray<Diagnostic>> GetEditorConfigDiagnostics(
        ImmutableArray<SyntaxTree> syntaxTrees,
        SyntaxTree configuredTree,
        string diagnosticId,
        ReportDiagnostic severity,
        bool automaticActivation)
    {
        var compilation = CSharpCompilation.Create(
            "AnalyzerConfigOptionsTest",
            syntaxTrees,
            SetUpFixture.ProjectReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSyntaxTreeOptionsProvider(new TestSyntaxTreeOptionsProvider(configuredTree, diagnosticId, severity)));
        return await compilation.WithAnalyzers(
            [new MessagingMigrationAnalyzer()],
            new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(
                    ImmutableArray<AdditionalText>.Empty,
                    new TestAnalyzerConfigOptionsProvider(automaticActivation)),
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: true)).GetAnalyzerDiagnosticsAsync();
    }

    static async Task<ImmutableArray<Diagnostic>> GetSeverityConfigDiagnostics(
        string source,
        string diagnosticId,
        ReportDiagnostic? severity = null,
        ReportDiagnostic? globalSeverity = null,
        ImmutableDictionary<string, string>? treeOptions = null,
        bool automaticActivation = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
        var optionsProvider = severity is null && globalSeverity is null
            ? null
            : new TestSyntaxTreeOptionsProvider(syntaxTree, diagnosticId, severity, globalSeverity);

        var compilation = CSharpCompilation.Create(
            "AnalyzerConfigOptionsTest",
            [syntaxTree],
            SetUpFixture.ProjectReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithSyntaxTreeOptionsProvider(optionsProvider));
        return await compilation.WithAnalyzers(
            [new MessagingMigrationAnalyzer()],
            new CompilationWithAnalyzersOptions(
                new AnalyzerOptions(
                    ImmutableArray<AdditionalText>.Empty,
                    new TestAnalyzerConfigOptionsProvider(automaticActivation, treeOptions)),
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false,
                reportSuppressedDiagnostics: true)).GetAnalyzerDiagnosticsAsync();
    }

    // dotnet_diagnostic severity is a compiler tree option, not an AnalyzerConfigOptions value.
    // SyntaxTreeOptionsProvider is the Roslyn API that retains the .editorconfig file scope.
    sealed class TestSyntaxTreeOptionsProvider(
        SyntaxTree configuredTree,
        string configuredDiagnosticId,
        ReportDiagnostic? configuredSeverity = null,
        ReportDiagnostic? configuredGlobalSeverity = null) : SyntaxTreeOptionsProvider
    {
        public override GeneratedKind IsGenerated(SyntaxTree tree, CancellationToken cancellationToken = default) => GeneratedKind.NotGenerated;

#pragma warning disable PS0003 // A parameter of type CancellationToken on a non-private delegate or method should be optional
        public override bool TryGetDiagnosticValue(
            SyntaxTree tree,
            string diagnosticId,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity)
        {
            if (tree == configuredTree &&
                diagnosticId == configuredDiagnosticId &&
                configuredSeverity is { } configuredTreeSeverity)
            {
                severity = configuredTreeSeverity;
                return true;
            }

            severity = ReportDiagnostic.Default;
            return false;
        }

        public override bool TryGetGlobalDiagnosticValue(
            string diagnosticId,
            CancellationToken cancellationToken,
            out ReportDiagnostic severity)
        {
            if (configuredGlobalSeverity is { } globalSeverity &&
                diagnosticId == configuredDiagnosticId)
            {
                severity = globalSeverity;
                return true;
            }

            severity = ReportDiagnostic.Default;
            return false;
        }
#pragma warning restore PS0003 // A parameter of type CancellationToken on a non-private delegate or method should be optional
    }

    sealed class TestAnalyzerConfigOptionsProvider(bool automaticActivation, ImmutableDictionary<string, string>? treeOptions = null) : AnalyzerConfigOptionsProvider
    {
        static readonly AnalyzerConfigOptions EmptyOptions = new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        static readonly AnalyzerConfigOptions AutomaticActivationOptions = new TestAnalyzerConfigOptions(
            ImmutableDictionary<string, string>.Empty.Add("build_property.PublishTrimmed", "true"));

        readonly AnalyzerConfigOptions treeConfigOptions = treeOptions is { Count: > 0 }
            ? new TestAnalyzerConfigOptions(treeOptions)
            : EmptyOptions;

        public override AnalyzerConfigOptions GlobalOptions => automaticActivation ? AutomaticActivationOptions : EmptyOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => treeConfigOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText text) => EmptyOptions;
    }

    sealed class TestAnalyzerConfigOptions(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value!);

        public override IEnumerable<string> Keys => values.Keys;
    }

    // ===== NSB0039: Safe object creation =====

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Publish(new MyEvent(), new PublishOptions())|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.Publish(new MyEvent(), new PublishOptions())|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_MessageProcessingContextReply()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await [|context.Reply(new MyMessage(), new ReplyOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionExtensionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionExtensionSendLocal()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.SendLocal(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionExtensionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Publish(new MyEvent())|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextExtensionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextExtensionSendLocal()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.SendLocal(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextExtensionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.Publish(new MyEvent())|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_MessageProcessingContextExtensionReply()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await [|context.Reply(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SagaReplyToOriginator()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                async Task Bar(IMessageHandlerContext context)
                {
                    await [|ReplyToOriginator(context, new MyMessage())|];
                }
            }

            class MySagaData : ContainSagaData { }
            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_SessionSendWithDestination()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send("destination", new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_DirectObjectCreation_PipelineContextSendWithDestination()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await [|context.Send("destination", new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_SealedTypeVariable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            sealed class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    MyMessage msg = new MyMessage();
                    await [|session.Send(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_UnsealedVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await [|session.Send(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_ValueType()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            struct MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    MyMessage msg = new MyMessage();
                    await [|session.Send(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_NullableValueType()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await [|session.Send(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0039_MessageCreatorCreateInstance()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, IMessageCreator creator)
                {
                    await [|session.Send(creator.CreateInstance<MyMessage>())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MessageCreatorCreateInstanceWithAction()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, IMessageCreator creator)
                {
                    await [|session.Send(creator.CreateInstance<MyMessage>(_ => { }))|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_UpdateMessageDirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    [|context.UpdateMessage(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    // ===== NSB0040: Potentially polymorphic =====

    [Test]
    public Task NSB0040_UpdateMessageVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class MyMessage : IMessage { }

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    var message = new MyMessage();
                    [|context.UpdateMessage(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageCreatedByMessageCreator()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context, IMessageCreator creator)
                {
                    [|context.UpdateMessage(creator.CreateInstance<IMyMessage>())|];
                }
            }

            public interface IMyMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageSealedVariable()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context, MyMessage message)
                {
                    [|context.UpdateMessage(message)|];
                }
            }

            public sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_InterfaceVariable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, IMessage msg)
                {
                    await [|session.Send(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_BaseClassVariable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class BaseMessage : IMessage { }
            class DerivedMessage : BaseMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session, BaseMessage msg)
                {
                    await [|session.Send(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UnsealedClassVariable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session, MyMessage msg)
                {
                    await [|session.Send(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_ReassignedVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    message = new MyMessage();
                    await [|session.Send(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_RefUseOfVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    Replace(ref message);
                    await [|session.Send(message)|];
                }

                static void Replace(ref MyMessage message) => message = new MyMessage();
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_NestedInvocationWithEarlierRefMutation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }
            class DerivedMessage : MyMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await Task.WhenAll(Mutate(ref message), [|session.Send(message)|]);
                }

                static Task Mutate(ref MyMessage message)
                {
                    message = new DerivedMessage();
                    return Task.CompletedTask;
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_ReceiverRefUseOfVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }
            class DerivedMessage : MyMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await [|Replace(ref message, session).Send(message)|];
                }

                static IMessageSession Replace(ref MyMessage message, IMessageSession session)
                {
                    message = new DerivedMessage();
                    return session;
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_LocalFunctionReceiverMutatesCapturedVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }
            class DerivedMessage : MyMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();

                    IMessageSession GetSession()
                    {
                        message = new DerivedMessage();
                        return session;
                    }

                    await [|GetSession().Send(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_EarlierArgumentRefUseOfVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage
            {
                public string Destination { get; } = "destination";
            }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await [|session.Send(message.Destination, message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_ConditionalVarInitializer()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session, bool condition)
                {
                    var message = condition ? new MyMessage() : new MyMessage();
                    await [|session.Send(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_InvocationVarInitializer()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = CreateMessage();
                    await [|session.Send(message)|];
                }

                MyMessage CreateMessage() => new MyMessage();
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MethodReturnType()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var msg = GetMessage();
                    await [|session.Send(msg)|];
                }

                MyMessage GetMessage() => new MyMessage();
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    // ===== Mutator context setters =====

    [Test]
    public Task NSB0039_MutatorIncomingContext_DirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context)
                {
                    [|context.Message = new MyMessage()|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MutatorOutgoingContext_DirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateOutgoingMessageContext context)
                {
                    [|context.OutgoingMessage = new MyEvent()|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MutatorIncomingContext_ValueType()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context, MyValue message)
                {
                    [|context.Message = message|];
                }
            }

            struct MyValue : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_MutatorIncomingContext_VarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context)
                {
                    var message = new MyMessage();
                    [|context.Message = message|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MutatorOutgoingContext_VarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateOutgoingMessageContext context)
                {
                    var message = new MyEvent();
                    [|context.OutgoingMessage = message|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MutatorIncomingContext_SealedVariable()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context, MyMessage message)
                {
                    [|context.Message = message|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MutatorOutgoingContext_CreatedByMessageCreator()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateOutgoingMessageContext context, IMessageCreator creator)
                {
                    [|context.OutgoingMessage = creator.CreateInstance<MyEvent>()|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NoDiagnostic_MutatorContext_ObjectType()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context)
                {
                    context.Message = new object();
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_MutatorContext_UnrelatedMessageProperty()
    {
        var source =
            """
            using NServiceBus;

            class Foo
            {
                public object Message { get; set; }

                void Bar(Foo foo)
                {
                    foo.Message = new MyMessage();
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    // ===== NSB0041: Generic T == object =====

    [Test]
    public Task NSB0041_GenericTIsObject_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object msg = new object();
                    await [|session.Send<object>(msg, new SendOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_SessionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object msg = new object();
                    await [|session.Publish<object>(msg, new PublishOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_PipelineContextSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    object msg = new object();
                    await [|context.Send<object>(msg, new SendOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_MessageProcessingContextReply()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    object msg = new object();
                    await [|context.Reply<object>(msg, new ReplyOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_SessionExtensionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object msg = new object();
                    await [|session.Send<object>(msg)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_SagaReplyToOriginator()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                async Task Bar(IMessageHandlerContext context)
                {
                    object msg = new object();
                    await [|ReplyToOriginator<object>(context, msg)|];
                }
            }

            class MySagaData : ContainSagaData { }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_UpdateMessage()
    {
        var source =
            """
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context, object message)
                {
                    [|context.UpdateMessage<object>(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    // ===== UpdateMessageInstance on IIncomingLogicalMessageContext =====

    [Test]
    public Task NSB0039_UpdateMessageInstanceDirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IIncomingLogicalMessageContext context)
                {
                    [|context.UpdateMessageInstance(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_UpdateMessageInstanceVarObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class MyMessage : IMessage { }

            class Foo
            {
                void Bar(IIncomingLogicalMessageContext context)
                {
                    var message = new MyMessage();
                    [|context.UpdateMessageInstance(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageInstanceCreatedByMessageCreator()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IIncomingLogicalMessageContext context, IMessageCreator creator)
                {
                    [|context.UpdateMessageInstance(creator.CreateInstance<IMyMessage>())|];
                }
            }

            public interface IMyMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageInstanceSealedVariable()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IIncomingLogicalMessageContext context, MyMessage message)
                {
                    [|context.UpdateMessageInstance(message)|];
                }
            }

            public sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0041_GenericTIsObject_UpdateMessageInstance()
    {
        var source =
            """
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IIncomingLogicalMessageContext context, object message)
                {
                    [|context.UpdateMessageInstance<object>(message)|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NoDiagnostic_UpdateMessageInstance_UnrelatedMethod()
    {
        var source =
            """
            using NServiceBus;

            class Helper
            {
                public void UpdateMessageInstance(object message) { }
            }

            class Foo
            {
                void Bar(Helper helper, MyMessage message)
                {
                    helper.UpdateMessageInstance(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NSB0039_TestableIncomingLogicalMessageContext_UpdateMessageInstance()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;

            class Foo
            {
                void Bar(TestableIncomingLogicalMessageContext context)
                {
                    [|context.UpdateMessageInstance(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    // ===== Method groups and delegates =====

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_SessionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, PublishOptions, CancellationToken, Task> publish = [|session.Publish|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_SessionExtensionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, CancellationToken, Task> send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_SessionSendLocal()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, CancellationToken, Task> sendLocal = [|session.SendLocal|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_PipelineContextSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IPipelineContext context)
                {
                    Func<MyMessage, SendOptions, Task> send = [|context.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_PipelineContextPublish()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IPipelineContext context)
                {
                    Func<MyMessage, PublishOptions, Task> publish = [|context.Publish|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_MessageProcessingContextReply()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageProcessingContext context)
                {
                    Func<MyMessage, ReplyOptions, Task> reply = [|context.Reply|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_SagaReplyToOriginator()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                void Bar(IMessageHandlerContext context)
                {
                    Func<IMessageHandlerContext, MyMessage, IReadOnlyDictionary<string, string>, Task> reply = [|ReplyToOriginator|];
                }
            }

            class MySagaData : ContainSagaData { }
            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_CustomDelegate_PositionalMessageParameter()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading;
            using System.Threading.Tasks;

            delegate Task MySendDelegate(MyMessage msg, SendOptions options, CancellationToken cancellationToken);

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    MySendDelegate send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_MethodGroup_SealedMessage_AsArgument()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    RegisterHandler([|session.Send|]);
                }

                void RegisterHandler(Func<MyMessage, SendOptions, CancellationToken, Task> handler) { }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_MethodGroup_InterfaceMessage_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<ICommand, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MethodGroup_UnsealedMessage_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<UnsealedMessage, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }

            class UnsealedMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MethodGroup_ObjectMessage_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<object, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_MethodGroup_ObjectMessage_SessionSendLocal()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<object, CancellationToken, Task> sendLocal = [|session.SendLocal|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0041_MethodGroup_ExplicitGenericObject_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<object, SendOptions, CancellationToken, Task> send = [|session.Send<object>|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0041_MethodGroup_IsReportedWithoutMigrationAudit()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<object, SendOptions, CancellationToken, Task> send = [|session.Send<object>|];
                }
            }
            """;
        return MigrationTest(source).AssertDiagnostics(DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NoDiagnostic_MethodGroup_ExplicitGenericNonObject()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = session.Send<MyMessage>;
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_MethodGroup_UnrelatedMethod()
    {
        var source =
            """
            using System;
            using System.Threading.Tasks;

            class Helper
            {
                public Task DoSomething(object message) => Task.CompletedTask;
            }

            class Foo
            {
                void Bar(Helper helper)
                {
                    Func<object, Task> action = helper.DoSomething;
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_MethodGroup_NonMessagingMember()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<Type, SubscribeOptions, CancellationToken, Task> subscribe = session.Subscribe;
                }
            }
            """;
        return Assert(source);
    }

    // ===== Concrete implementations and testing fakes =====

    [Test]
    public Task NSB0039_TestableMessageSession_DirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_TestableMessageSession_InterfaceMessage()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageSession session, IMessage message)
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0041_TestableMessageSession_ExplicitGenericObject()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageSession session, object message)
                {
                    await [|session.Send<object>(message, new SendOptions())|];
                }
            }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task NSB0039_TestableMessageSession_MethodGroup()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(TestableMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_TestablePipelineContext_Send()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestablePipelineContext context)
                {
                    await [|context.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_TestablePipelineContext_Publish()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestablePipelineContext context)
                {
                    await [|context.Publish(new MyEvent(), new PublishOptions())|];
                }
            }

            class MyEvent : IEvent { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_TestableMessageProcessingContext_Reply()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageProcessingContext context)
                {
                    await [|context.Reply(new MyMessage(), new ReplyOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_TestableOutgoingLogicalMessageContext_UpdateMessage()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Testing;

            class Foo
            {
                void Bar(TestableOutgoingLogicalMessageContext context)
                {
                    [|context.UpdateMessage(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_CustomMessageSession_DirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_CustomMessageSession_InterfaceMessage()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session, IMessage message)
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0039_CustomMessageSession_MethodGroup()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                void Bar(CustomMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_CustomMessageSession_ImplementationOnBaseClass()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class DerivedMessageSession : CustomMessageSession { }

            class Foo
            {
                async Task Bar(DerivedMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0039_ExplicitMessageSession_InvokedThroughInterface()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class ExplicitMessageSession : IMessageSession
            {
                Task IMessageSession.Send(object message, SendOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
                Task IMessageSession.Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
                Task IMessageSession.Publish(object message, PublishOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
                Task IMessageSession.Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
                Task IMessageSession.Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
                Task IMessageSession.Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NoDiagnostic_NonContractSession()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading;
            using System.Threading.Tasks;

            class NotAMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(NotAMessageSession session)
                {
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics();
    }

    [Test]
    public Task NoDiagnostic_ImplementingSession_UnrelatedOverload()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send(object message) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics();
    }

    // ===== Generic forwarding =====

    [Test]
    public Task NSB0040_GenericForwarding_Unconstrained()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Forward<T>(T message, IMessageSession session)
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_GenericForwarding_ClassConstraint()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Forward<T>(T message, IMessageSession session) where T : class
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_GenericForwarding_InterfaceConstraint()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Forward<T>(T message, IMessageSession session) where T : IMyMessage
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }

            interface IMyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_GenericForwarding_MethodGroup()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Forward<T>(T message, IMessageSession session) where T : class
                {
                    Func<T, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    // ===== Inferred versus explicit object =====

    [Test]
    public Task NoDiagnostic_InferredObjectArgument_10xObjectOnlyOverload()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, object message)
                {
                    await session.Send(message, new SendOptions());
                }
            }
            """;
        return Assert(source);
    }

    // ===== Expression trees =====

    [Test]
    public Task NSB0039_ExpressionTree_SealedMessage()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Linq.Expressions;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Expression<Func<MyMessage, Task>> expression = message => [|session.Send(message)|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_ExpressionTree_UnsealedMessage()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Linq.Expressions;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Expression<Func<MyMessage, Task>> expression = message => [|session.Send(message)|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source, DiagnosticIds.RuntimeTypeMayDiffer);
    }

    // ===== Activation =====

    [Test]
    public Task NSB0041_IsReportedWithoutMigrationAudit()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object message = new MyMessage();
                    await [|session.Send<object>(message)|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).AssertDiagnostics(DiagnosticIds.GenericMessageTypeIsObject);
    }

    [Test]
    public Task MigrationDiagnostics_AreInactiveByDefault()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).AssertDiagnostics();
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForPublishTrimmed()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).WithProperty("build_property.PublishTrimmed", "true")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForPublishTrimmed_EnablesNSB0040()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await [|session.Send(message)|];
                }
            }
            """;
        return MigrationTest(source).WithProperty("build_property.PublishTrimmed", "true")
            .AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForPublishAot()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).WithProperty("build_property.PublishAot", "true")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForIsAotCompatible()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).WithProperty("build_property.IsAotCompatible", "true")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForIsTrimmable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).WithProperty("build_property.IsTrimmable", "true")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForEnableTrimAnalyzer()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await [|session.Send(new MyMessage())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source).WithProperty("build_property.EnableTrimAnalyzer", "true")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForNSB0039EditorConfigSeverityOnly()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return AssertEditorConfigSeverity(
            source,
            DiagnosticIds.UseGenericMessageType,
            ReportDiagnostic.Info,
            automaticActivation: false,
            DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AreEnabledForNSB0040EditorConfigSeverityOnly()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return AssertEditorConfigSeverity(
            source,
            DiagnosticIds.RuntimeTypeMayDiffer,
            ReportDiagnostic.Warn,
            automaticActivation: false,
            DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_RespectsNSB0039NoneSeverity()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return AssertEditorConfigSeverity(
            source,
            DiagnosticIds.UseGenericMessageType,
            ReportDiagnostic.Suppress,
            automaticActivation: true,
            DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_RespectsNSB0040NoneSeverity()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return AssertEditorConfigSeverity(
            source,
            DiagnosticIds.RuntimeTypeMayDiffer,
            ReportDiagnostic.Suppress,
            automaticActivation: true,
            DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_DefaultSeverityFallsBackToAutomatic()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, int? message)
                {
                    await session.Send(new MyMessage());
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return AssertEditorConfigSeverity(
            source,
            DiagnosticIds.UseGenericMessageType,
            ReportDiagnostic.Default,
            automaticActivation: true,
            DiagnosticIds.UseGenericMessageType,
            DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public async Task MigrationDiagnostics_SeverityIsScopedToConfiguredSyntaxTree()
    {
        const string configuredSource =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class ConfiguredFoo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new ConfiguredMessage());
                }
            }

            class ConfiguredMessage : IMessage { }
            """;
        const string unconfiguredSource =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class UnconfiguredFoo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new UnconfiguredMessage());
                }
            }

            class UnconfiguredMessage : IMessage { }
            """;
        var configuredTree = CSharpSyntaxTree.ParseText(configuredSource, path: "Configured.cs");
        var unconfiguredTree = CSharpSyntaxTree.ParseText(unconfiguredSource, path: "Unconfigured.cs");

        var diagnostics = await GetEditorConfigDiagnostics(
            [configuredTree, unconfiguredTree],
            configuredTree,
            DiagnosticIds.UseGenericMessageType,
            ReportDiagnostic.Suppress,
            automaticActivation: true);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo([DiagnosticIds.UseGenericMessageType]));
        NUnit.Framework.Assert.That(diagnostics[0].Location.SourceTree?.FilePath, Is.EqualTo("Unconfigured.cs"));
    }

    // The global channel (TryGetGlobalDiagnosticValue) was invisible to the old implementation, so
    // global severities that enable the diagnostics were ignored by the analyzer gate.

    [Test]
    public async Task MigrationDiagnostics_AutomaticActivation_RespectsGlobalNoneSeverity()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            globalSeverity: ReportDiagnostic.Suppress,
            automaticActivation: true);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.Empty);
    }

    [Test]
    public async Task MigrationDiagnostics_GlobalSeverity_EnablesWithoutAutomaticActivation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            globalSeverity: ReportDiagnostic.Warn,
            automaticActivation: false);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo([DiagnosticIds.UseGenericMessageType]));
    }

    [Test]
    public async Task MigrationDiagnostics_AutomaticActivation_RespectsBulkCategoryNoneSeverity()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            treeOptions: ImmutableDictionary<string, string>.Empty.Add(
                "dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "none"),
            automaticActivation: true);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.Empty);
    }

    [Test]
    public async Task MigrationDiagnostics_BulkCategorySeverity_EnablesWithoutAutomaticActivation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            treeOptions: ImmutableDictionary<string, string>.Empty.Add(
                "dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning"),
            automaticActivation: false);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo([DiagnosticIds.UseGenericMessageType]));
    }

    [Test]
    public async Task MigrationDiagnostics_ExplicitPerRuleDefaultSeverity_BlocksBulkConfiguration()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            severity: ReportDiagnostic.Default,
            treeOptions: ImmutableDictionary<string, string>.Empty.Add(
                "dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning"),
            automaticActivation: false);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.Empty);
    }

    [Test]
    public async Task MigrationDiagnostics_ExplicitGlobalDefaultSeverity_BlocksBulkConfiguration()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            globalSeverity: ReportDiagnostic.Default,
            treeOptions: ImmutableDictionary<string, string>.Empty.Add(
                "dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning"),
            automaticActivation: false);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.Empty);
    }

    [Test]
    public async Task MigrationDiagnostics_ExplicitPerRuleDefaultSeverity_StillHonorsAutomaticActivation()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;
        var diagnostics = await GetSeverityConfigDiagnostics(
            source,
            DiagnosticIds.UseGenericMessageType,
            severity: ReportDiagnostic.Default,
            automaticActivation: true);

        NUnit.Framework.Assert.That(diagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo([DiagnosticIds.UseGenericMessageType]));
    }

    // ===== Negative tests =====

    [Test]
    public Task NoDiagnostic_ObjectCreation_NewObject()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new object(), new SendOptions());
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ObjectCreation_TargetTypedNewObject()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new(), new SendOptions());
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ObjectVariable()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session, object message)
                {
                    await session.Send(message);
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_AnonymousType()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new { Value = 1 };
                    await session.Send(message);
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitGenericNonObject()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send<MyMessage>(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ActionTOverload()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send<MyMessage>(_ => { }, new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_Subscribe()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Subscribe(typeof(MyEvent), new SubscribeOptions());
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_Unsubscribe()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Unsubscribe(typeof(MyEvent), new UnsubscribeOptions());
                }
            }

            class MyEvent : IEvent { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_UnrelatedMethod()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await SomeOtherMethod(new MyMessage());
                }

                Task SomeOtherMethod(object msg) => Task.CompletedTask;
            }

            class MyMessage : IMessage { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ForwardCurrentMessageTo()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await context.ForwardCurrentMessageTo("destination");
                }
            }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_RequestTimeout()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                async Task Bar(IMessageHandlerContext context)
                {
                    await RequestTimeout<MyTimeout>(context, TimeSpan.FromSeconds(10));
                }
            }

            class MySagaData : ContainSagaData { }
            class MyTimeout { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_SessionSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object message = new MyMessage();
                    await session.Send(message, typeof(IMyInterface), new SendOptions());
                }
            }

            interface IMyInterface : IMessage { }
            class MyMessage : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_SessionPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object message = new MyEvent();
                    await session.Publish(message, typeof(IMyInterface), new PublishOptions());
                }
            }

            interface IMyInterface : IEvent { }
            class MyEvent : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_SessionSendLocal()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object message = new MyMessage();
                    await session.SendLocal(message, typeof(IMyInterface));
                }
            }

            interface IMyInterface : IMessage { }
            class MyMessage : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_SessionSendDestination()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    object message = new MyMessage();
                    await session.Send("destination", message, typeof(IMyInterface));
                }
            }

            interface IMyInterface : IMessage { }
            class MyMessage : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_PipelineContextSend()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    object message = new MyMessage();
                    await context.Send(message, typeof(IMyInterface), new SendOptions());
                }
            }

            interface IMyInterface : IMessage { }
            class MyMessage : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_PipelineContextPublish()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    object message = new MyEvent();
                    await context.Publish(message, typeof(IMyInterface), new PublishOptions());
                }
            }

            interface IMyInterface : IEvent { }
            class MyEvent : IMyInterface { }
            """;
        return Assert(source);
    }

    [Test]
    public Task NoDiagnostic_ExplicitType_MessageProcessingContextReply()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading.Tasks;
            using System;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    object message = new MyReply();
                    await context.Reply(message, typeof(IMyInterface), new ReplyOptions());
                }
            }

            interface IMyInterface : IMessage { }
            class MyReply : IMyInterface { }
            """;
        return Assert(source);
    }

    // ===== UpdateMessage method groups =====

    [Test]
    public Task NSB0040_UpdateMessageMethodGroup_SealedMessage()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;
            using System;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    Action<SealedMessage> update = [|context.UpdateMessage|];
                }
            }

            sealed class SealedMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageMethodGroup_ObjectMessage()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;
            using System;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    Action<object> update = [|context.UpdateMessage|];
                }
            }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0040_UpdateMessageMethodGroup_TestableFake()
    {
        var source =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;
            using NServiceBus.Testing;
            using System;

            class Foo
            {
                void Bar(TestableOutgoingLogicalMessageContext context)
                {
                    Action<SealedMessage> update = [|context.UpdateMessage|];
                }
            }

            sealed class SealedMessage : IMessage { }
            """;
        return FakeMigrationTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    // ===== Renamed implementation parameters =====

    [Test]
    public Task NSB0039_CustomMessageSession_RenamedParameter_DirectObjectCreation()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session)
                {
                    await [|session.Send(new MyMessage(), new SendOptions())|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_CustomMessageSession_RenamedParameter_InterfaceArgument()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session, IMessage message)
                {
                    await [|session.Send(message, new SendOptions())|];
                }
            }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0039_CustomMessageSession_RenamedParameter_MethodGroup()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                void Bar(CustomMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }

            sealed class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task NSB0040_CustomMessageSession_RenamedParameter_MethodGroupInterface()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                void Bar(CustomMessageSession session)
                {
                    Func<ICommand, SendOptions, CancellationToken, Task> send = [|session.Send|];
                }
            }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task NSB0041_CustomMessageSession_RenamedParameter_ExplicitGenericObject()
    {
        var source =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(T payload, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(T payload, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session, object message)
                {
                    await [|session.Send<object>(message, new SendOptions())|];
                }
            }
            """;
        return MigrationAuditTest(source).AssertDiagnostics(DiagnosticIds.GenericMessageTypeIsObject);
    }

    // ===== Interface-map gate =====

    [Test]
    public Task NoDiagnostic_InterfaceRichType_UnrelatedSendMethod()
    {
        var source =
            """
            using NServiceBus;
            using System.Threading;
            using System.Threading.Tasks;

            interface IMarkerA { void A(); }
            interface IMarkerB { void B(); }
            interface IMarkerC : IMarkerA, IMarkerB { void C(); }

            class InterfaceRichType : IMarkerC
            {
                public void A() { }
                public void B() { }
                public void C() { }
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(InterfaceRichType session)
                {
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationAuditTest(source).AssertDiagnostics();
    }
}

#pragma warning restore NUnit1034
