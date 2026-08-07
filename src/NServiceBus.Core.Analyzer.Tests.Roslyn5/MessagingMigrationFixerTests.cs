#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using NServiceBus.Core.Analyzer.Fixes;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class MessagingMigrationFixerTests : CodeFixTestFixture<MessagingMigrationAnalyzer, MessagingMigrationFixer>
{
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
}

#pragma warning restore NUnit1034
