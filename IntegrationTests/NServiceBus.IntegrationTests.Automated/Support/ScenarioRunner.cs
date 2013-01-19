namespace NServiceBus.IntegrationTests.Automated.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class ScenarioRunner
    {
        public static void Run(IEnumerable<BehaviorDescriptor> behaviorDescriptors)
        {
            var transportsToRunTestOn = GetTransportsToRunTestOn();
            
            foreach (var transport in transportsToRunTestOn)
            {
                Console.Out.WriteLine("Running test for transport: {0}",string.IsNullOrEmpty(transport)?"User defined":transport.Split(',').FirstOrDefault());

                var runners = InitatializeRunners(behaviorDescriptors, transport);

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

        static List<string> GetTransportsToRunTestOn()
        {
            var transportsToRunTestOn = new List<string>();

            //------------- this part is NSB specific, we should create an extension point to not interfer with users 
            var frame = new StackFrame(3);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            var attribute =
                type.GetCustomAttributes(typeof (ForAllTransportsAttribute), true).FirstOrDefault() as ForAllTransportsAttribute;

            if (attribute != null)
            {
                transportsToRunTestOn = attribute.Transports.Select(t => t.GetType().AssemblyQualifiedName).ToList();
            }

            // -----------------------

            if (!transportsToRunTestOn.Any())
                transportsToRunTestOn.Add(null);
            return transportsToRunTestOn;
        }

        static void PerformScenarios(List<ActiveRunner> runners)
        {
            var endpoints = runners.Select(r => r.Instance).ToList();

            StartEndpoints(endpoints);

            var startTime = DateTime.UtcNow;
            var maxTime = TimeSpan.FromSeconds(30);

            var tasks = new List<Task>();

            foreach (var endpoint in endpoints)
            {
                // Should I define task creation options long running?
                tasks.Add(Task.Factory.StartNew(() => SpinWait.SpinUntil(() => endpoint.Done(), maxTime)));
            }

            Task.WaitAll(tasks.ToArray());

            if ((DateTime.UtcNow - startTime) > maxTime)
                Assert.Fail(GenerateTestTimedOutMessage(endpoints, maxTime));
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
            foreach (var endpoint in endpoints)
            {
                Task.Factory.StartNew(
                    () =>
                    {
                        endpoint.Start();
                        endpoint.ApplyWhens();
                    });
            }
        }

        static List<ActiveRunner> InitatializeRunners(IEnumerable<BehaviorDescriptor> behaviorDescriptors, string transport)
        {
            var runners = new List<ActiveRunner>();

            foreach (var descriptor in behaviorDescriptors)
            {
                var runner = PrepareRunner(descriptor.Factory.Get());

                Assert.True(runner.Instance.Initialize(descriptor.Factory.GetType().AssemblyQualifiedName, descriptor.Context, transport),
                            "Endpoint {0} failed to initalize", runner.Instance.Name());

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