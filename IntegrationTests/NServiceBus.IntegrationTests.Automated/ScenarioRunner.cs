namespace NServiceBus.IntegrationTests.Automated
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class ScenarioRunner
    {
        public static void Run(params IScenarioFactory[] scenarioFactory)
        {
            var endpoints = new List<EndpointRunner>();

            foreach (var endpointScenario in scenarioFactory)
            {
                var runner = PrepareRunner(endpointScenario.Get());

                Assert.True(runner.Initialize(endpointScenario.GetType().AssemblyQualifiedName), "Endpoint {0} failed to initalize", runner.Name());

                endpoints.Add(runner);
            }





            foreach (var endpoint in endpoints)
            {
                Task.Factory.StartNew(
                    () => Assert.True(endpoint.Start(), "Endpoint {0} failed to start", endpoint.Name()));

            }


            bool done = false;
            IEnumerable<string> failures = new List<string>();

            var startTime = DateTime.UtcNow;
            var maxTime = TimeSpan.FromSeconds(10);


            while (!done)
            {
                done = endpoints.All(e => e.Done());

                //get failure so that we can abort early
                failures = endpoints.SelectMany(e => e.FailedAssertions());

                if (failures.Any())
                {
                    done = true;
                }

                Thread.Sleep(500);

                Assert.True((DateTime.UtcNow - startTime) < maxTime);
            }

            Assert.True(!failures.Any(),string.Join(";",failures));

        }

        static EndpointRunner PrepareRunner(EndpointScenario endpointScenario)
        {

            var domainSetup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                };



            var appDomain = AppDomain.CreateDomain(endpointScenario.EndpointName, AppDomain.CurrentDomain.Evidence, domainSetup);

            return (EndpointRunner)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(EndpointRunner).FullName);


        }
    }
}