namespace NServiceBus.Core.Analyzer.Tests;

using System.Threading.Tasks;
using NServiceBus.Core.Analyzer.Fixes;
using NServiceBus.Core.Analyzer.Handlers;
using NUnit.Framework;
using Particular.AnalyzerTesting;

[TestFixture]
public class AddConventionBasedHandleMethodFixerTests : CodeFixTestFixture<HandlerAttributeAnalyzer, AddConventionBasedHandleMethodFixer>
{
    [Test]
    public Task AddsConventionBasedHandleMethodWithOptionalCancellationToken()
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
            using System.Threading.Tasks;
            using System.Threading;

            [Handler]
            class NonHandler
            {
                public async Task Handle(MyMessage message, IMessageHandlerContext context, CancellationToken cancellationToken = default)
                {
                    await Task.CompletedTask;
                }
            }
            """;

        return Assert(original, expected, mustCompile: false);
    }
}