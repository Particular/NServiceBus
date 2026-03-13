namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using NServiceBus.Core.Analyzer.Fixes;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class AddIHandleMessagesInterfaceFixerTests : CodeFixTestFixture<HandlerAttributeAnalyzer, AddIHandleMessagesInterfaceFixer>
{
    [Test]
    public Task AddsIHandleMessagesWithMyMessagePlaceholder()
    {
        var original =
            """
            using NServiceBus;

            [Handler]
            class NonHandler
            {
            }
            """;

        var expected =
            """
            using System.Threading.Tasks;
            using NServiceBus;

            [Handler]
            class NonHandler
            : IHandleMessages<MyMessage>
            {
                public async Task Handle(MyMessage message, IMessageHandlerContext context)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        return Assert(original, expected, mustCompile: false);
    }
}