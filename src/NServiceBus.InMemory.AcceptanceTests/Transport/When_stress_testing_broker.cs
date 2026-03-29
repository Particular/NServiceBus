using System;

namespace NServiceBus.AcceptanceTests.Transport;

using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using AcceptanceTesting.Customization;
using EndpointTemplates;
using NUnit.Framework;

public class When_stress_testing_broker : NServiceBusAcceptanceTest
{
    [Test, CancelAfter(10_000)]
    public async Task Should_handle_parallel_sends(CancellationToken cancellationToken = default)
    {
        var messageCount = 100;

        var context = await Scenario.Define<Context>(c =>
        {
            var allIds = Enumerable.Range(0, messageCount).Select(i => $"msg-{i}").ToList();
            c.ExpectedMessageIds = new ConcurrentDictionary<string, string>(allIds.ToDictionary(x => x, x => x));
        })
        .WithEndpoint<Sender>(b => b.When(async session =>
        {
            var tasks = Enumerable.Range(0, messageCount)
                .Select(i =>
                {
                    var options = new SendOptions();
                    options.SetMessageId($"msg-{i}");
                    return session.Send(new TestMessage { Number = i }, options, cancellationToken);
                })
                .ToArray();
            await Task.WhenAll(tasks);
        }))
        .WithEndpoint<Receiver>()
        .Run(cancellationToken);

        Assert.That(context.ExpectedMessageIds.IsEmpty, Is.True, $"Not all messages were received. Missing: {string.Join(",", context.ExpectedMessageIds.Keys)}");
    }

    [Test, CancelAfter(10_000)]
    public async Task Should_handle_cascading_sends_from_handlers(CancellationToken cancellationToken = default)
    {
        var initialMessageCount = 50;
        var cascadingMessagesPerInitial = 5;
        var totalExpectedMessages = initialMessageCount * cascadingMessagesPerInitial;

        var context = await Scenario.Define<Context>(c =>
        {
            var allIds = Enumerable.Range(0, totalExpectedMessages).Select(i => $"cascade-{i}").ToList();
            c.ExpectedMessageIds = new ConcurrentDictionary<string, string>(allIds.ToDictionary(x => x, x => x));
        })
        .WithEndpoint<Sender>(b => b.When(async session =>
        {
            var tasks = Enumerable.Range(0, initialMessageCount)
                .Select(i =>
                {
                    var options = new SendOptions();
                    options.SetMessageId($"initial-{i}");
                    return session.Send(new KickoffMessage { SequenceNumber = i }, options, cancellationToken);
                })
                .ToArray();
            await Task.WhenAll(tasks);
        }))
        .WithEndpoint<Receiver>()
        .WithEndpoint<FinalReceiver>()
        .Run(cancellationToken);

        Assert.That(context.ExpectedMessageIds.IsEmpty, Is.True, $"Not all cascading messages were received. Missing: {string.Join(",", context.ExpectedMessageIds.Keys.Take(10))}...");
    }

    public class Context : ScenarioContext
    {
        public ConcurrentDictionary<string, string> ExpectedMessageIds { get; set; } = new();
    }

    public class Sender : EndpointConfigurationBuilder
    {
        public Sender() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(20);
                var routing = c.ConfigureRouting();
                routing.RouteToEndpoint(typeof(TestMessage), Conventions.EndpointNamingConvention(typeof(Receiver)));
                routing.RouteToEndpoint(typeof(KickoffMessage), Conventions.EndpointNamingConvention(typeof(Receiver)));
            });
    }

    public class Receiver : EndpointConfigurationBuilder
    {
        public Receiver() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(20);
                c.ConfigureRouting().RouteToEndpoint(typeof(CascadingMessage), Conventions.EndpointNamingConvention(typeof(FinalReceiver)));
            });

        [Handler]
        public class TestMessageHandler(Context testContext) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                if (!testContext.ExpectedMessageIds.TryRemove(context.MessageId, out _))
                {
                    testContext.MarkAsFailed(new Exception($"Received unexpected message with ID {context.MessageId}"));
                }
                testContext.MarkAsCompleted(testContext.ExpectedMessageIds.IsEmpty);
                return Task.CompletedTask;
            }
        }

        [Handler]
        public class KickoffHandler : IHandleMessages<KickoffMessage>
        {
            public async Task Handle(KickoffMessage message, IMessageHandlerContext context)
            {
                // Send multiple cascading messages in parallel
                // Use sequential IDs: message 0 sends cascade-0 to cascade-4, message 1 sends cascade-5 to cascade-9, etc.
                var tasks = Enumerable.Range(0, 5)
                    .Select(i =>
                    {
                        var options = new SendOptions();
                        var messageId = $"cascade-{message.SequenceNumber * 5 + i}";
                        options.SetMessageId(messageId);
                        return context.Send(new CascadingMessage
                        {
                            OriginalSequenceNumber = message.SequenceNumber,
                            CascadingSequenceNumber = message.SequenceNumber * 5 + i
                        }, options);
                    })
                    .ToArray();

                await Task.WhenAll(tasks);
            }
        }
    }

    public class FinalReceiver : EndpointConfigurationBuilder
    {
        public FinalReceiver() =>
            EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(20));

        [Handler]
        public class CascadingHandler(Context testContext) : IHandleMessages<CascadingMessage>
        {
            public Task Handle(CascadingMessage message, IMessageHandlerContext context)
            {
                if (!testContext.ExpectedMessageIds.TryRemove(context.MessageId, out _))
                {
                    testContext.MarkAsFailed(new Exception($"Received unexpected message with ID {context.MessageId}"));
                }
                testContext.MarkAsCompleted(testContext.ExpectedMessageIds.IsEmpty);
                return Task.CompletedTask;
            }
        }
    }

    public class TestMessage : IMessage
    {
        public int Number { get; set; }
    }

    public class KickoffMessage : IMessage
    {
        public int SequenceNumber { get; set; }
    }

    public class CascadingMessage : IMessage
    {
        public int OriginalSequenceNumber { get; set; }
        public int CascadingSequenceNumber { get; set; }
    }
}