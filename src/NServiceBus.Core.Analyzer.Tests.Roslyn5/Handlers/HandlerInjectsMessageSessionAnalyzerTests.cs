#nullable enable

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class HandlerInjectsMessageSessionAnalyzerTests : AnalyzerTestFixture<HandlerInjectsMessageSessionAnalyzer>
{
    [Test]
    public Task ReportsDiagnosticForInterfaceBasedHandlerWithSessionInConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler([|IMessageSession|] session) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage { }
            """;

        return Assert(source, DiagnosticIds.HandlerInjectsMessageSession);
    }

    [Test]
    public Task ReportsDiagnosticForInterfaceBasedHandlerWithSessionAsProperty()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler : IHandleMessages<MyMessage>
            {
                public [|IMessageSession|] Session { get; set; }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage { }
            """;

        return Assert(source, DiagnosticIds.HandlerInjectsMessageSession);
    }

    [Test]
    public Task ReportsDiagnosticForConventionBasedHandlerWithSessionInConstructor()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler
            {
                public MyHandler([|IMessageSession|] session) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService { }
            class MyMessage : IMessage { }
            """;

        return Assert(source, DiagnosticIds.HandlerInjectsMessageSession);
    }

    [Test]
    public Task ReportsDiagnosticForConventionBasedHandlerWithSessionAsProperty()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler
            {
                public [|IMessageSession|] Session { get; set; }
                public Task Handle(MyMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService { }
            class MyMessage : IMessage { }
            """;

        return Assert(source, DiagnosticIds.HandlerInjectsMessageSession);
    }

    [Test]
    public Task DoesNotReportForInterfaceBasedHandlerWithoutSession()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler : IHandleMessages<MyMessage>
            {
                public MyHandler(IMyService service) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            interface IMyService { }
            class MyMessage : IMessage { }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForConventionBasedHandlerWithoutSession()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class MyHandler
            {
                public MyHandler(IMyService service) { }
                public Task Handle(MyMessage message, IMessageHandlerContext context, IMyService service) => Task.CompletedTask;
            }

            interface IMyService { }
            class MyMessage : IMessage { }
            """;

        return Assert(source);
    }

    [Test]
    public Task DoesNotReportForNonHandlerClass()
    {
        var source =
            """
            using NServiceBus;

            class MyService
            {
                public MyService(IMessageSession session) { }
            }
            """;

        return Assert(source);
    }
}