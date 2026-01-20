#pragma warning disable NUnit1034 // Base TestFixtures should be abstract

namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using Helpers;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;

[TestFixture]
public class HandlerAttributeAnalyzerTests : AnalyzerTestFixture<HandlerAttributeAnalyzer>
{
    [Test]
    public Task ReportsMissingAttributeOnLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            class [|MyHandler|] : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(DiagnosticIds.HandlerAttributeMissing, source);
    }

    [Test]
    public Task DoesNotReportWhenAttributePresent()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [HandlerAttribute]
            class MyHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(source);
    }

    [Test]
    public Task ReportsMissingAttributeOnDerivedLeafHandler()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            class [|ConcreteHandler|] : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(DiagnosticIds.HandlerAttributeMissing, source);
    }

    [Test]
    public Task ReportsMisplacedAttributeOnAbstractBase()
    {
        var source =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [[|HandlerAttribute|]]
            abstract class BaseHandler : IHandleMessages<MyMessage>
            {
                public Task Handle(MyMessage message, IMessageHandlerContext context) => Task.CompletedTask;
            }

            [HandlerAttribute]
            class ConcreteHandler : BaseHandler
            {
            }

            class MyMessage : IMessage
            {
            }
            """;

        return Assert(DiagnosticIds.HandlerAttributeMisplaced, source);
    }
}