namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ScenarioRunner
    {
        public static void Run(ScenarioDescriptor scenarioDescriptor, IEnumerable<BehaviorDescriptor> behaviorDescriptors)
        {
            foreach (RunDescriptor runDescriptor in scenarioDescriptor)
            {
                Console.Out.WriteLine("Running test for : {0}", runDescriptor.Name);
                PrintSettings(runDescriptor.Settings);

                var runners = InitatializeRunners(runDescriptor, behaviorDescriptors);

                try
                {
                    PerformScenarios(runners);

                    Console.Out.WriteLine("Result: Successfull");
                    Console.Out.WriteLine("------------------------------------------------------");
                }
                finally
                {
                    foreach (var runner in runners)
                    {
                        AppDomain.Unload(runner.AppDomain);
                    }
                }    
            }
        }

        private static void PrintSettings(IEnumerable<KeyValuePair<string, string>> settings)
        {
            Console.WriteLine("Using settings:");
            foreach (KeyValuePair<string, string> pair in settings)
            {
                Console.Out.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        static void PerformScenarios(IEnumerable<ActiveRunner> runners)
        {
            var endpoints = runners.Select(r => r.Instance).ToList();

            StartEndpoints(endpoints);

            var startTime = DateTime.UtcNow;
            var maxTime = TimeSpan.FromSeconds(30);

            Task.WaitAll(endpoints.Select(endpoint => Task.Factory.StartNew(() => SpinWait.SpinUntil(endpoint.Done, maxTime))).Cast<Task>().ToArray());

            try
            {
                if ((DateTime.UtcNow - startTime) > maxTime)
                {
                    throw new ScenarioException(GenerateTestTimedOutMessage(endpoints, maxTime));
                }
            }
            finally
            {
                StopEndpoints(endpoints);
            }
        }

        static string GenerateTestTimedOutMessage(List<EndpointRunner> endpoints, TimeSpan maxTime)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("The maximum time limit for this test({0}s) has been reached",
                                        maxTime.TotalSeconds));
            sb.AppendLine("----------------------------------------------------------------------------");
            sb.AppendLine("Endpoint statuses:");
            endpoints.ForEach(e =>
                {
                    sb.AppendLine(string.Format("{0} - {1}", e.Name(), e.Done() ? "Done" : "Not done"));
                });

            return sb.ToString();
        }

        static void StartEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(() =>
                {
                    endpoint.Start();
                    endpoint.ApplyWhens();
                })).ToArray();

            Task.WaitAll(tasks);
        }

        static void StopEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(endpoint.Stop)).ToArray();

            Task.WaitAll(tasks);
        }

        static List<ActiveRunner> InitatializeRunners(RunDescriptor runDescriptor, IEnumerable<BehaviorDescriptor> behaviorDescriptors)
        {
            var runners = new List<ActiveRunner>();

            foreach (var descriptor in behaviorDescriptors)
            {
                var runner = PrepareRunner(descriptor.Factory.Get());

                if (!runner.Instance.Initialize(
                        descriptor.Factory.GetType().AssemblyQualifiedName, descriptor.Context, runDescriptor.Settings))
                {
                    throw new ScenarioException(string.Format("Endpoint {0} failed to initalize", runner.Instance.Name()));
                }

                runners.Add(runner);
            }
            return runners;
        }

        static ActiveRunner PrepareRunner(EndpointBehavior endpointBehavior)
        {
            var domainSetup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase
                };

            var appDomain = AppDomain.CreateDomain(endpointBehavior.EndpointName, AppDomain.CurrentDomain.Evidence, domainSetup);

            return new ActiveRunner
                {
                    Instance = (EndpointRunner)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(EndpointRunner).FullName),
                    AppDomain = appDomain
                };
        }
    }

    class ActiveRunner
    {
        public EndpointRunner Instance { get; set; }
        public AppDomain AppDomain { get; set; }
    }
}