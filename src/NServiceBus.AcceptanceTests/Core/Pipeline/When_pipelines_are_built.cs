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

#if NET5_0_OR_GREATER
            // System.Threading.Tasks.Task has changed to System.Threading.Tasks.Task`1[System.Threading.Tasks.VoidTaskResult] in .net5
            // This ifdef is to make sure the new type is only validated for .net5 or greater.
            var scenario = "net5";
#else
            var scenario = string.Empty;
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