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

        // IMessageProcessingContext
        [TestCase("IMessageProcessingContext", "obj.Reply(new object(), new ReplyOptions());")]
        [TestCase("IMessageProcessingContext", "obj.Reply<object>(_ => { }, new ReplyOptions());")]
        [TestCase("IMessageProcessingContext", "obj.ForwardCurrentMessageTo(\"destination\");")]

        // IMessageProcessingContextExtensions
        [TestCase("IMessageProcessingContext", "obj.Reply(new object());")]
        [TestCase("IMessageProcessingContext", "obj.Reply<object>(_ => { });")]

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
        public Task DiagnosticIsReportedForCorePublicMethods(string type, string call)
        {
            var source =
$@"using NServiceBus; 
using System;
using System.Threading.Tasks; 
class Foo
{{
    void Bar({type} obj)
    {{
        {call}
    }}
}}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 9) },
            };

            return Verify(source, expected);
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
        public Task DiagnosticIsReportedForUniformSession(string call)
        {
            var source =
$@"using NServiceBus;
using NServiceBus.UniformSession;
class Foo
{{
    void Bar(IUniformSession session)
    {{
        {call}
    }}
}}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 9) },
            };

            return Verify(source, expected);
        }

        [TestCase("RequestTimeout<object>(context, DateTime.Now);")]
        [TestCase("RequestTimeout<object>(context, DateTime.Now, new object());")]
        [TestCase("RequestTimeout<object>(context, TimeSpan.Zero);")]
        [TestCase("RequestTimeout<object>(context, TimeSpan.Zero, new object());")]
        [TestCase("ReplyToOriginator(context, new object());")]
        public Task DiagnosticIsReportedForSagaProtectedMethods(string call)
        {
            var source =
$@"using System;
using NServiceBus;
class TestSaga : Saga<Data>
{{
    void Bar(IMessageHandlerContext context)
    {{
        {call}
    }}

    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Data> mapper) {{ }}
}}
class Data : ContainSagaData {{}}
";
            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 9) },
            };
            return Verify(source, expected);
        }

        [Test]
        public Task DiagnosticIsReportedForAsyncMethods()
        {
            var source =
@"using System.Threading.Tasks;
using NServiceBus;
class Foo
{
    async Task Bar(IPipelineContext ctx)
    {
        ctx.Send(new object(), new SendOptions());
    }
}";

            var expected = new DiagnosticResult
            {
                Id = "NSB0001",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 9) },
            };

            return Verify(source, expected);
        }

        [TestCase(
@"// 1
using NServiceBus;
using System.Threading.Tasks;
class Foo
{
    async Task Bar(IMessageHandlerContext context)
    {
        await context.Send(null);
    }
}",
            Description = "because the task is awaited.")]
        [TestCase(
@"// 2
using NServiceBus;
using System.Threading.Tasks;
class Foo
{
    Task Bar(IMessageHandlerContext context) =>
        context.Send(null);
}",
            Description = "because the task is returned.")]
        [TestCase(
@"// 3
using NServiceBus;
using System.Threading.Tasks;
class Foo
{
    void Bar(IMessageSession session)
    {
        session.Send(null).GetAwaiter().GetResult();
    }
}",
            Description = "because the send operation task is accessed.")]
        [TestCase(
@"// 4
using NServiceBus;
class Foo
{
    void Bar(IMessageSession session)
    {
        session.Send(null).Wait();
    }
}",
            Description = "because the send operation task is accessed.")]
        [TestCase(
@"// 5
using System.Threading.Tasks;
class Foo
{
    void Bar(object message)
    {
        Task.Run(() => {});
    }
}",
            Description = "because a non-NSB method is called.")]
        [TestCase(
@"// 6
using System;
using System.Threading.Tasks;
using NServiceBus;

class Foo
{
    void Bar(object message)
    {
        Send(new object(), new SendOptions());
    }

    Task Send(object message, SendOptions options)
    {
        throw new NotImplementedException();
    }
}",
            Description = "because a non-NSB method is called.")]
        [TestCase(
@"// 7
using NServiceBus;
class Foo
{
    void Bar(IMessageSession session)
    {
        var status = session.Send(null).Status;
    }
}",
            Description = "because the send operation task is accessed.")]
        public Task NoDiagnosticIsReported(string source) => Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new AwaitOrCaptureTasksAnalyzer();
    }
}
