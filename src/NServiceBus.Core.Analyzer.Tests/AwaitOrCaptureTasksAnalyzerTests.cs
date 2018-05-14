namespace NServiceBus.Core.Analyzer.Tests
{
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class AwaitOrCaptureTasksAnalyzerTests : DiagnosticVerifier
    {
        // IPipelineContext
        [TestCase("IPipelineContext", "obj.Send(new object(), new SendOptions());")]
        [TestCase("IPipelineContext", "obj.Send<object>(_ => { }, new SendOptions());")]
        [TestCase("IPipelineContext", "obj.Publish(new object(), new PublishOptions());")]
        [TestCase("IPipelineContext", "obj.Publish<object>(_ => { }, new PublishOptions());")]

        // IPipelineContextExtensions
        [TestCase("IPipelineContext", "obj.Send(new object());")]
        [TestCase("IPipelineContext", "obj.Send<object>(_ => { });")]
        [TestCase("IPipelineContext", "obj.Send(\"destination\", new object());")]
        [TestCase("IPipelineContext", "obj.Send<object>(\"destination\", _ => { });")]
        [TestCase("IPipelineContext", "obj.SendLocal(new object());")]
        [TestCase("IPipelineContext", "obj.SendLocal<object>(_ => { });")]
        [TestCase("IPipelineContext", "obj.Publish(new object());")]
        [TestCase("IPipelineContext", "obj.Publish<object>();")]
        [TestCase("IPipelineContext", "obj.Publish<object>(_ => { });")]

        // IMessageSession
        [TestCase("IMessageSession", "obj.Send(new object(), new SendOptions());")]
        [TestCase("IMessageSession", "obj.Send<object>(_ => { }, new SendOptions());")]
        [TestCase("IMessageSession", "obj.Publish(new object(), new PublishOptions());")]
        [TestCase("IMessageSession", "obj.Publish<object>(_ => { }, new PublishOptions());")]
        [TestCase("IMessageSession", "obj.Subscribe(typeof(object), new SubscribeOptions());")]
        [TestCase("IMessageSession", "obj.Unsubscribe(typeof(object), new UnsubscribeOptions());")]

        // IMessageSessionExtensions
        [TestCase("IMessageSession", "obj.Send(new object());")]
        [TestCase("IMessageSession", "obj.Send<object>(_ => { });")]
        [TestCase("IMessageSession", "obj.Send(\"destination\", new object());")]
        [TestCase("IMessageSession", "obj.Send<object>(\"destination\", _ => { });")]
        [TestCase("IMessageSession", "obj.SendLocal(new object());")]
        [TestCase("IMessageSession", "obj.SendLocal<object>(_ => { });")]
        [TestCase("IMessageSession", "obj.Publish(new object());")]
        [TestCase("IMessageSession", "obj.Publish<object>();")]
        [TestCase("IMessageSession", "obj.Publish<object>(_ => { });")]
        [TestCase("IMessageSession", "obj.Subscribe(typeof(object));")]
        [TestCase("IMessageSession", "obj.Subscribe<object>();")]
        [TestCase("IMessageSession", "obj.Unsubscribe(typeof(object));")]
        [TestCase("IMessageSession", "obj.Unsubscribe<object>();")]
        public async Task DiagnosticIsReported(string type, string call)
        {
            var source =
$@"using NServiceBus;
public class Foo
{{
    public void Bar({type} obj)
    {{
        {call}
    }}
}}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Message = "Expression calling an NServiceBus method creates a Task that is not awaited or assigned to a variable.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 9) },
            };

            await Verify(source, expected);
        }

        [TestCase("RequestTimeout<TimeoutMessage>(context, DateTime.Now);")]
        [TestCase("RequestTimeout<TimeoutMessage>(context, DateTime.Now, new TimeoutMessage());")]
        [TestCase("RequestTimeout<TimeoutMessage>(context, TimeSpan.Zero);")]
        [TestCase("RequestTimeout<TimeoutMessage>(context, TimeSpan.Zero, new TimeoutMessage());")]
        [TestCase("ReplyToOriginator(context, new object());")]
        public async Task DiagnosticsIsReportedForSagaAPIs(string api)
        {
            var source =
                $@"using System;
using System.Threading.Tasks;
using NServiceBus;
class TestSaga : Saga<TestSagaData>, IHandleMessages<TestMessage>
{{
    public Task Handle(TestMessage message, IMessageHandlerContext context)
    {{
        {api}
        return Task.FromResult(0);
    }}

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
    {{
    }}
}}";
            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Message = "Expression calling an NServiceBus method creates a Task that is not awaited or assigned to a variable.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 9) },
            };

            await Verify(source, expected);

        }

        [TestCase("")]
        [TestCase(
@"using NServiceBus;
using System.Threading.Tasks;
public class TestHandler : IHandleMessages<TestMessage>
{
    public async Task Handle(object message, IMessageHandlerContext context)
    {
        context.Send(new object(), new SendOptions());
        return Task.FromResult(0);
    }
}")]
        [TestCase(
@"using NServiceBus;
using System.Threading.Tasks;
public class TestHandler : IHandleMessages<TestMessage>
{
    public async Task Handle(object message, IMessageHandlerContext context)
    {
        await context.Send(new object(), new SendOptions());
    }
}")]
        [TestCase(
@"using NServiceBus;
using System.Threading.Tasks;
public class TestHandler : IHandleMessages<TestMessage>
{
    public Task Handle(object message, IMessageHandlerContext context) =>
        context.Send(new object(), new SendOptions());
}")]
        public async Task NoDiagnosticIsReported(string source) => await Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new AwaitOrCaptureTasksAnalyzer();
    }
}
