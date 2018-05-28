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

        // Endpoint
        [TestCase("EndpointConfiguration", "Endpoint.Create(obj);")]
        [TestCase("EndpointConfiguration", "Endpoint.Start(obj);")]

        // IStartableEndpoint
        [TestCase("IStartableEndpoint", "obj.Start();")]

        // IEndpointInstance
        [TestCase("IEndpointInstance", "obj.Stop();")]
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

        [TestCase("session.Send(new object());")]
        [TestCase("session.Send(new object(), new SendOptions());")]
        [TestCase("session.Send<object>(_ => { }, new SendOptions());")]
        [TestCase("session.Send<object>(_ => { });")]
        [TestCase("session.Send(\"destination\", new object());")]
        [TestCase("session.Send<object>(\"destination\", _ => { });")]
        [TestCase("session.SendLocal(new object());")]
        [TestCase("session.SendLocal<object>(_ => { });")]
        [TestCase("session.Publish(new object());")]
        [TestCase("session.Publish(new object(), new PublishOptions());")]
        [TestCase("session.Publish<object>();")]
        [TestCase("session.Publish<object>(_ => { });")]
        [TestCase("session.Publish<object>(_ => { }, new PublishOptions());")]
        public async Task DiagnosticIsReportedForUniformSessionAPI(string api)
        {
            var source =
$@"using NServiceBus;
using NServiceBus.UniformSession;
public class Foo
{{
    public void Bar(IUniformSession session)
    {{
        {api}
    }}
}}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Message = "Expression calling an NServiceBus method creates a Task that is not awaited or assigned to a variable.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 9) },
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

        [Test]
        public async Task DiagnosticsIsReportedForAsyncMethods()
        {
            var source =
@"using NServiceBus;
public class Foo
{
    public async Task Bar(IPipelineContext ctx)
    {
        ctx.Send(new object(), new SendOptions());
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Message = "Expression calling an NServiceBus method creates a Task that is not awaited or assigned to a variable.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 9) },
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
        await context.Send(new object(), new SendOptions());
    }
}", Description = "because send task is awaited.")]
        [TestCase(
@"using NServiceBus;
using System.Threading.Tasks;
public class TestHandler : IHandleMessages<TestMessage>
{
    public Task Handle(object message, IMessageHandlerContext context) =>
        context.Send(new object(), new SendOptions());
}", Description = "because send task is returned.")]
        [TestCase(
            @"using NServiceBus;
using System.Threading.Tasks;
public class Program
{
    public void SendSync(object message, IMessageSession session)
    {
        session.Send(message).GetAwaiter().GetResult();
    }
}", Description = "because send task is used.")]
        [TestCase(
            @"using NServiceBus;
public class Program
{
    public void SendSync(object message, IMessageSession session)
    {
        session.Send(message).Wait();
    }
}", Description = "because send task is used.")]
        [TestCase(
            @"using NServiceBus;
using System.Threading.Tasks;
public class Program
{
    public void SendSync(object message, IMessageSession session)
    {
        session.Send(message).ConfigureAwait(false);
    }
}", Description = "because send task is used.")]
        [TestCase(
            @"using NServiceBus;
using System.Threading.Tasks;
public class Program
{
    public void Handle(object message, IMessageSession session)
    {
        Task.Run(() => {});
    }
}", Description = "because non-NSB API is called.")]
        public async Task NoDiagnosticIsReported(string source) => await Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new AwaitOrCaptureTasksAnalyzer();
    }
}
