#nullable enable

#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
                    await [|session.Send(new MyMessage())|];
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source)
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Info)
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
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
                    await [|session.Send(message)|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source)
            .WithDiagnosticSeverity(DiagnosticIds.RuntimeTypeMayDiffer, ReportDiagnostic.Warn)
            .AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
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
                    await [|session.Send(message)|];
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Suppress)
            .AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
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
                    await [|session.Send(new MyMessage())|];
                    await session.Send(message);
                }
            }

            class MyMessage : IMessage { }
            """;
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.RuntimeTypeMayDiffer, ReportDiagnostic.Suppress)
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_DefaultSeverityFallsBackToAutomatic()
    {
        const string source =
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
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Default)
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_DefaultSeverityOnNSB0039KeepsNSB0040Enabled()
    {
        const string source =
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
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Default)
            .AssertDiagnostics(DiagnosticIds.RuntimeTypeMayDiffer);
    }

    [Test]
    public Task MigrationDiagnostics_SeverityIsScopedToConfiguredSyntaxTree()
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
                    await [|session.Send(new UnconfiguredMessage())|];
                }
            }

            class UnconfiguredMessage : IMessage { }
            """;
        return AnalyzerTest.ForAnalyzer<MessagingMigrationAnalyzer>()
            .WithSource(configuredSource, "Configured.cs")
            .WithSource(unconfiguredSource, "Unconfigured.cs")
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Suppress, "Configured.cs")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    // The global severity channel (TryGetGlobalDiagnosticValue) was previously invisible to the
    // analyzer gate, so global severities that enable these diagnostics were ignored.

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_RespectsGlobalNoneSeverity()
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
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithGlobalDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Suppress)
            .AssertDiagnostics();
    }

    [Test]
    public Task MigrationDiagnostics_GlobalSeverity_EnablesWithoutAutomaticActivation()
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
        return MigrationTest(source)
            .WithGlobalDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Warn)
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_AutomaticActivation_RespectsBulkCategoryNoneSeverity()
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
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithEditorConfigOption("dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "none")
            .AssertDiagnostics();
    }

    [Test]
    public Task MigrationDiagnostics_BulkCategorySeverity_EnablesWithoutAutomaticActivation()
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
        return MigrationTest(source)
            .WithEditorConfigOption("dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning")
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
    }

    [Test]
    public Task MigrationDiagnostics_ExplicitPerRuleDefaultSeverity_BlocksBulkConfiguration()
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
        return MigrationTest(source)
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Default)
            .WithEditorConfigOption("dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning")
            .AssertDiagnostics();
    }

    [Test]
    public Task MigrationDiagnostics_ExplicitGlobalDefaultSeverity_BlocksBulkConfiguration()
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
        return MigrationTest(source)
            .WithGlobalDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Default)
            .WithEditorConfigOption("dotnet_analyzer_diagnostic.category-NServiceBus.Code.severity", "warning")
            .AssertDiagnostics();
    }

    [Test]
    public Task MigrationDiagnostics_ExplicitPerRuleDefaultSeverity_StillHonorsAutomaticActivation()
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
        return MigrationTest(source)
            .WithProperty("build_property.PublishTrimmed", "true")
            .WithDiagnosticSeverity(DiagnosticIds.UseGenericMessageType, ReportDiagnostic.Default)
            .AssertDiagnostics(DiagnosticIds.UseGenericMessageType);
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
