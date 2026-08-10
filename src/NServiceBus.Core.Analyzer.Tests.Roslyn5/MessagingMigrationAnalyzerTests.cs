#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

    static async Task AssertEditorConfigOption(string source, params string[] expectedDiagnosticIds)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
        var compilation = CSharpCompilation.Create(
            "AnalyzerConfigOptionsTest",
            [syntaxTree],
            SetUpFixture.ProjectReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzerOptions = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new TestAnalyzerConfigOptionsProvider(syntaxTree));
        var analyzerDiagnostics = await compilation.WithAnalyzers(
            [new MessagingMigrationAnalyzer()],
            new CompilationWithAnalyzersOptions(
                analyzerOptions,
                onAnalyzerException: null,
                concurrentAnalysis: true,
                logAnalyzerExecutionTime: false)).GetAnalyzerDiagnosticsAsync();

        NUnit.Framework.Assert.That(analyzerDiagnostics.Select(diagnostic => diagnostic.Id), Is.EquivalentTo(expectedDiagnosticIds));
    }

    sealed class TestAnalyzerConfigOptionsProvider(SyntaxTree configuredTree) : AnalyzerConfigOptionsProvider
    {
        static readonly AnalyzerConfigOptions EmptyOptions = new TestAnalyzerConfigOptions(ImmutableDictionary<string, string>.Empty);
        static readonly AnalyzerConfigOptions MigrationOptions = new TestAnalyzerConfigOptions(
            ImmutableDictionary<string, string>.Empty.Add(
                "nservicebus_enable_message_overload_migration_diagnostics",
                "true"));

        public override AnalyzerConfigOptions GlobalOptions => EmptyOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            tree == configuredTree ? MigrationOptions : EmptyOptions;

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
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage());
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
    public Task MigrationDiagnostics_AreEnabledForEditorConfigOption()
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
        return AssertEditorConfigOption(
            source,
            DiagnosticIds.UseGenericMessageType,
            DiagnosticIds.RuntimeTypeMayDiffer);
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
}

#pragma warning restore NUnit1034
