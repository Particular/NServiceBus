#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NServiceBus.Core.Analyzer.Fixes;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class MessagingMigrationFixerTests : CodeFixTestFixture<MessagingMigrationAnalyzer, MessagingMigrationFixer>
{
    static readonly MetadataReference TestingFakesReference =
        MetadataReference.CreateFromFile(typeof(NServiceBus.Testing.TestableMessageSession).Assembly.Location);

    protected override void ConfigureFixtureTests(CodeFixTest test)
    {
        base.ConfigureFixtureTests(test);
        test.WithProperty("build_property.PublishTrimmed", "true");
        test.AddReferences(TestingFakesReference);
    }

    [Test]
    public Task SessionPublish()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Publish(new MyEvent(), new PublishOptions());
                }
            }

            class MyEvent : IEvent { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Publish<MyEvent>(new MyEvent(), new PublishOptions());
                }
            }

            class MyEvent : IEvent { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task SessionSend()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
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

        return Assert(original, expected);
    }

    [Test]
    public Task UnsealedVarObjectCreation()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await session.Send(message);
                }
            }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MyMessage : IMessage { }

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    var message = new MyMessage();
                    await session.Send<MyMessage>(message);
                }
            }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task PipelineContextSend()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Send<MyMessage>(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task PipelineContextSendLocal()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.SendLocal(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.SendLocal<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MessageProcessingContextReply()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await context.Reply(new MyMessage(), new ReplyOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await context.Reply<MyMessage>(new MyMessage(), new ReplyOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task SessionExtensionSend()
    {
        var original =
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

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task SessionExtensionSendLocal()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.SendLocal(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.SendLocal<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task SessionExtensionPublish()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Publish(new MyEvent());
                }
            }

            class MyEvent : IEvent { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Publish<MyEvent>(new MyEvent());
                }
            }

            class MyEvent : IEvent { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task PipelineContextExtensionSend()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Send(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Send<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task PipelineContextExtensionPublish()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Publish(new MyEvent());
                }
            }

            class MyEvent : IEvent { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IPipelineContext context)
                {
                    await context.Publish<MyEvent>(new MyEvent());
                }
            }

            class MyEvent : IEvent { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MessageProcessingContextExtensionReply()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await context.Reply(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageProcessingContext context)
                {
                    await context.Reply<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task NoFixForNewObject()
    {
        var original =
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

        return Assert(original, original);
    }

    [Test]
    public Task NoFixForTargetTypedNewObject()
    {
        var original =
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

        return Assert(original, original);
    }

    [Test]
    public Task SagaReplyToOriginator()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                async Task Bar(IMessageHandlerContext context)
                {
                    await ReplyToOriginator(context, new MyMessage());
                }
            }

            class MySagaData : ContainSagaData { }
            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class MySaga : Saga<MySagaData>
            {
                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<MySagaData> mapper) { }

                async Task Bar(IMessageHandlerContext context)
                {
                    await ReplyToOriginator<MyMessage>(context, new MyMessage());
                }
            }

            class MySagaData : ContainSagaData { }
            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task SessionSendWithDestination()
    {
        var original =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send("destination", new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(IMessageSession session)
                {
                    await session.Send<MyMessage>("destination", new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task UpdateMessage()
    {
        var original =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    context.UpdateMessage(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using NServiceBus.Pipeline;

            class Foo
            {
                void Bar(IOutgoingLogicalMessageContext context)
                {
                    context.UpdateMessage<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MutatorIncomingContext_Message()
    {
        var original =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context)
                {
                    context.Message = new MyMessage();
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context)
                {
                    context.UpdateMessageInstance<MyMessage>(new MyMessage());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MutatorOutgoingContext_OutgoingMessage()
    {
        var original =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateOutgoingMessageContext context)
                {
                    context.OutgoingMessage = new MyEvent();
                }
            }

            class MyEvent : IEvent { }
            """;

        var expected =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateOutgoingMessageContext context)
                {
                    context.UpdateMessage<MyEvent>(new MyEvent());
                }
            }

            class MyEvent : IEvent { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MutatorIncomingContext_MessageValueType()
    {
        var original =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context, MyValue message)
                {
                    context.Message = message;
                }
            }

            struct MyValue : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using NServiceBus.MessageMutator;

            class Foo
            {
                void Bar(MutateIncomingMessageContext context, MyValue message)
                {
                    context.UpdateMessageInstance<MyValue>(message);
                }
            }

            struct MyValue : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MethodGroup_SessionSend()
    {
        var original =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = session.Send;
                }
            }

            sealed class MyMessage : IMessage { }
            """;

        var expected =
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

        return Assert(original, expected);
    }

    [Test]
    public Task MethodGroup_SessionSendLocal()
    {
        var original =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, CancellationToken, Task> sendLocal = session.SendLocal;
                }
            }

            sealed class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    Func<MyMessage, CancellationToken, Task> sendLocal = session.SendLocal<MyMessage>;
                }
            }

            sealed class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task MethodGroup_AsArgument()
    {
        var original =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    RegisterHandler(session.Send);
                }

                void RegisterHandler(Func<MyMessage, SendOptions, CancellationToken, Task> handler) { }
            }

            sealed class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System;
            using System.Threading;
            using System.Threading.Tasks;

            class Foo
            {
                void Bar(IMessageSession session)
                {
                    RegisterHandler(session.Send<MyMessage>);
                }

                void RegisterHandler(Func<MyMessage, SendOptions, CancellationToken, Task> handler) { }
            }

            sealed class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task NoFixForExplicitGenericMethodGroup()
    {
        var original =
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

            class MyMessage : IMessage { }
            """;

        return Assert(original, original);
    }

    [Test]
    public Task NoFixForDIMReliantConcreteImplementation()
    {
        var original =
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
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, original);
    }

    [Test]
    public Task NoFixForDIMReliantConcreteImplementation_MethodGroup()
    {
        var original =
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
                    Func<MyMessage, SendOptions, CancellationToken, Task> send = session.Send;
                }
            }

            sealed class MyMessage : IMessage { }
            """;

        return Assert(original, original);
    }

    [Test]
    public Task ConcreteImplementationWithGenericOverload()
    {
        var original =
            """
            using NServiceBus;
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                [OverloadResolutionPriority(-1)]
                public Task Send<T>(T message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                [OverloadResolutionPriority(-1)]
                public Task Publish<T>(T message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session)
                {
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using System;
            using System.Runtime.CompilerServices;
            using System.Threading;
            using System.Threading.Tasks;

            class CustomMessageSession : IMessageSession
            {
                public Task Send(object message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                [OverloadResolutionPriority(-1)]
                public Task Send<T>(T message, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                [OverloadResolutionPriority(-1)]
                public Task Publish<T>(T message, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Publish<T>(Action<T> messageConstructor, PublishOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
                public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }

            class Foo
            {
                async Task Bar(CustomMessageSession session)
                {
                    await session.Send<MyMessage>(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }

    [Test]
    public Task TestableMessageSession()
    {
        var original =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageSession session)
                {
                    await session.Send(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        var expected =
            """
            using NServiceBus;
            using NServiceBus.Testing;
            using System.Threading.Tasks;

            class Foo
            {
                async Task Bar(TestableMessageSession session)
                {
                    await session.Send<MyMessage>(new MyMessage(), new SendOptions());
                }
            }

            class MyMessage : IMessage { }
            """;

        return Assert(original, expected);
    }
}

#pragma warning restore NUnit1034
