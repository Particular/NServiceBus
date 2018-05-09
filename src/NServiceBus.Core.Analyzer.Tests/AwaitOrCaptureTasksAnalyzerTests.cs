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
        [TestCase("context.Send(new object(), new SendOptions());")]
        [TestCase("context.Send<object>(_ => { }, new SendOptions());")]
        [TestCase("context.Publish(new object(), new PublishOptions());")]
        [TestCase("context.Publish<object>(_ => { }, new PublishOptions());")]
        public async Task DiagnosticIsReported(string call)
        {
            var source =
$@"using NServiceBus;
using System.Threading.Tasks;
public class TestHandler : IHandleMessages<TestMessage>
{{
    public Task Handle(object message, IMessageHandlerContext context)
    {{
        {call}
        return Task.FromResult(0);
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
        public async Task NoDiagnosticIsReported(string source) => await Verify(source);

        protected override DiagnosticAnalyzer GetAnalyzer() => new AwaitOrCaptureTasksAnalyzer ();
    }
}
