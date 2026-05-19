#nullable enable

namespace NServiceBus.AcceptanceTests.Core.LoggingIntegration;

using System;
using System.Linq;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NServiceBus.Pipeline;
using NUnit.Framework;
using ILoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;
using Conventions = AcceptanceTesting.Customization.Conventions;

[NonParallelizable]
public class When_using_endpoint_logging_scope : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_expose_endpoint_metadata_via_begin_endpoint_scope()
    {
        var context = await Scenario.Define<Context>(ctx => ctx.IncludeLoggingScopes = true)
            .WithEndpoint<EndpointWithScope>(b => b
                .ServiceResolve(static (provider, ctx, _) =>
                {
                    var endpointScope = provider.GetRequiredService<EndpointLoggingScope>();
                    ctx.ResolvedEndpointName = endpointScope.EndpointName;
                    ctx.ResolvedEndpointIdentifier = endpointScope.EndpointIdentifier;

                    var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    var logger = loggerFactory.CreateLogger("ScopeTest");
                    using (logger.BeginEndpointScope(endpointScope))
                    {
                        logger.LogInformation("Message inside logging scope");
                    }

                    ctx.MarkAsCompleted();
                    return Task.CompletedTask;
                }, afterStart: true))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.ResolvedEndpointName, Is.EqualTo(Conventions.EndpointNamingConvention(typeof(EndpointWithScope))));
            Assert.That(context.ResolvedEndpointIdentifier, Is.EqualTo("UsingEndpointLoggingScope.EndpointWithScope0"));
            Assert.That(context.Logs, Has.One.Matches<ScenarioContext.LogItem>(l =>
                l.LoggerName == "ScopeTest" &&
                (l.Message ?? string.Empty).Contains("Message inside logging scope") &&
                (l.Message ?? string.Empty).Contains("Endpoint = UsingEndpointLoggingScope.EndpointWithScope, EndpointIdentifier = UsingEndpointLoggingScope.EndpointWithScope0")));
        }
    }

    [Test]
    public async Task Should_not_duplicate_scope_when_inside_pipeline()
    {
        var context = await Scenario.Define<Context>(ctx => ctx.IncludeLoggingScopes = true)
            .WithEndpoint<EndpointWithBehavior>(b => b
                .When(async (session, _) =>
                {
                    await session.SendLocal(new TestMessage());
                }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            // Handler calls BeginEndpointScope — should not duplicate the pipeline slot scope
            var handlerMessage = context.Logs.FirstOrDefault(l => l.LoggerName.EndsWith("TestHandler") && (l.Message ?? string.Empty).Contains("Handler executed"));
            Assert.That(handlerMessage?.Message, Is.Not.Null);
            Assert.That(CountOccurrences(handlerMessage!.Message!, "Endpoint = UsingEndpointLoggingScope.EndpointWithBehavior"), Is.EqualTo(1),
                "Handler: endpoint scope should appear exactly once, not duplicated by BeginEndpointScope");

            // Outgoing behavior calls BeginEndpointScope — should not duplicate either
            var behaviorMessage = context.Logs.FirstOrDefault(l => l.LoggerName.EndsWith("OutgoingLoggingBehavior") && (l.Message ?? string.Empty).Contains("Behavior executed"));
            Assert.That(behaviorMessage?.Message, Is.Not.Null);
            Assert.That(CountOccurrences(behaviorMessage!.Message!, "Endpoint = UsingEndpointLoggingScope.EndpointWithBehavior"), Is.EqualTo(1),
                "Behavior: endpoint scope should appear exactly once from pipeline slot, not duplicated by BeginEndpointScope");
        }
    }

    static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    public class Context : ScenarioContext
    {
        public string? ResolvedEndpointName { get; set; }
        public object? ResolvedEndpointIdentifier { get; set; }
    }

    class EndpointWithScope : EndpointConfigurationBuilder
    {
        public EndpointWithScope() => EndpointSetup<DefaultServer>();
    }

    public class EndpointWithBehavior : EndpointConfigurationBuilder
    {
        public EndpointWithBehavior() => EndpointSetup<DefaultServer>(c =>
            c.Pipeline.Register<OutgoingLoggingBehavior>("Logs in outgoing pipeline to verify no scope duplication"));

        [Handler]
        public class TestHandler(ILogger<TestHandler> logger, Context testContext, EndpointLoggingScope endpointScope) : IHandleMessages<TestMessage>
        {
            public Task Handle(TestMessage message, IMessageHandlerContext context)
            {
                using (logger.BeginEndpointScope(endpointScope))
                {
                    logger.LogInformation("Handler executed");
                }
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }

        class OutgoingLoggingBehavior(EndpointLoggingScope endpointScope, ILogger<OutgoingLoggingBehavior> logger) : Behavior<IOutgoingLogicalMessageContext>
        {
            public override Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
            {
                using (logger.BeginEndpointScope(endpointScope))
                {
                    logger.LogInformation("Behavior executed");
                }

                return next();
            }
        }
    }

    public class TestMessage : IMessage { }
}