namespace NServiceBus.AcceptanceTests.Core.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using Particular.Approvals;

    public class When_pipelines_are_built : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_preserve_order()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<RegularEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run()
                .ConfigureAwait(false);

            var pipelineLogs = context.Logs.Where(x => x.LoggerName.StartsWith("NServiceBus.Pipeline"))
                .Distinct(LoggerNameComparer.Instance).Select(x => x.Message);

            // The output varies between TFMs, so a separate approval file is created for each framework.
            // If a TFM gets added to the test project in the future, it intentionally will create a new
            // "unknown" approval file, which will fail. The test should be updated to handle the new TFM.
#if NET472
            var scenario = "net472";
#elif NETCOREAPP3_1
            var scenario = "netcoreapp3.1";
#elif NET6_0
            var scenario = "net6.0";
#else
            var scenario = "unknown";
#endif
            Approver.Verify(string.Join(Environment.NewLine, pipelineLogs), scenario: scenario);
        }

        class LoggerNameComparer : IEqualityComparer<ScenarioContext.LogItem>
        {
            public static LoggerNameComparer Instance = new LoggerNameComparer();

            public bool Equals(ScenarioContext.LogItem x, ScenarioContext.LogItem y)
            {
                return x?.LoggerName == y?.LoggerName;
            }

            public int GetHashCode(ScenarioContext.LogItem obj)
            {
                return obj.LoggerName != null ? obj.LoggerName.GetHashCode() : 0;
            }
        }

        class Context : ScenarioContext
        {
        }

        class RegularEndpoint : EndpointConfigurationBuilder
        {
            public RegularEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }
        }
    }
}