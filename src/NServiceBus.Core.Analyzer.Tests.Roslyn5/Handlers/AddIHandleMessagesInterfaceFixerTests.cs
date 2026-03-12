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
            using NServiceBus;

            [Handler]
            class NonHandler
            : IHandleMessages<MyMessage>
            {
            }
            """;

        return Assert(original, expected, mustCompile: false);
    }
}