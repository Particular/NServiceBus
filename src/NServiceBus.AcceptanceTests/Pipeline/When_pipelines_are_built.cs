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

            Approver.Verify(string.Join(Environment.NewLine, pipelineLogs));
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