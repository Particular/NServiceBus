namespace NServiceBus.IntegrationTests.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ScenarioRunner
    {
        public static void Run(IList<RunDescriptor> runDescriptors, IList<BehaviorDescriptor> behaviorDescriptors, IList<IScenarioVerification> shoulds, Func<BehaviorContext, bool> done)
        {
            var totalRuns = runDescriptors.Count();

            var cts = new CancellationTokenSource();

            var po = new ParallelOptions
                {
                    CancellationToken = cts.Token
                };

            var maxParallelismSetting = Environment.GetEnvironmentVariable("max_test_parallelism");
            int maxParallelism;
            if (int.TryParse(maxParallelismSetting, out maxParallelism))
            {
                Console.Out.WriteLine("Parallelism limited to: {0}",maxParallelism);

                po.MaxDegreeOfParallelism = maxParallelism;
            }

            var results = new ConcurrentBag<RunSummary>();

            try
            {
                Parallel.ForEach(runDescriptors, po, runDescriptor =>
                    {
                        if (po.CancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        Console.Out.WriteLine("{0} - Started", runDescriptor.Key);

                        var runResult = PerformTestRun(behaviorDescriptors, shoulds, runDescriptor, done);

                        Console.Out.WriteLine("{0} - Finished", runDescriptor.Key);

                        results.Add(new RunSummary
                            {
                                Result = runResult,
                                RunDescriptor = runDescriptor,
                                Endpoints = behaviorDescriptors
                            });


                        if (runResult.Failed)
                        {
                            cts.Cancel();
                        }
                    });
            }
            catch (OperationCanceledException)
            {
                Console.Out.WriteLine("Testrun aborted due to test failures");
            }

            var failedRuns = results.Where(s => s.Result.Failed).ToList();

            foreach (var runSummary in failedRuns)
            {
                DisplayRunResult(runSummary, totalRuns);
            }

            if (failedRuns.Any())
                throw new AggregateException("Test run failed due to one or more exception", failedRuns.Select(f => f.Result.Exception));


            foreach (var runSummary in results.Where(s => !s.Result.Failed))
            {
                DisplayRunResult(runSummary, totalRuns);
            }
        }

        class RunSummary
        {
            public RunResult Result { get; set; }

            public RunDescriptor RunDescriptor { get; set; }

            public IEnumerable<BehaviorDescriptor> Endpoints { get; set; }
        }

        static void DisplayRunResult(RunSummary summary, int totalRuns)
        {
            var runDescriptor = summary.RunDescriptor;
            var runResult = summary.Result;

            Console.Out.WriteLine("------------------------------------------------------");
            Console.Out.WriteLine("Test summary for: {0}", runDescriptor.Key);
            if (totalRuns > 1)
                Console.Out.WriteLine(" - Permutation: {0}({1})", runDescriptor.Permutation, totalRuns);
            Console.Out.WriteLine("");


            PrintSettings(runDescriptor.Settings);

            Console.WriteLine("");
            Console.WriteLine("Endpoints:");

            foreach (var endpoint in runResult.ActiveEndpoints)
            {
                Console.Out.WriteLine("     - {0}", endpoint);
            }


            if (runResult.Failed)
                Console.Out.WriteLine("Test failed: {0}", runResult.Exception);
            else
            {
                Console.Out.WriteLine("Result: Successfull - Duration: {0}", runResult.TotalTime);
                Console.Out.WriteLine("------------------------------------------------------");

            }
        }

        static RunResult PerformTestRun(IList<BehaviorDescriptor> behaviorDescriptors, IList<IScenarioVerification> shoulds, RunDescriptor runDescriptor, Func<BehaviorContext, bool> done)
        {
            var runResult = new RunResult();

            var runTimer = new Stopwatch();

            runTimer.Start();

            try
            {
                List<ActiveRunner> runners = InitializeRunners(runDescriptor, behaviorDescriptors);

                try
                {
                    runResult.ActiveEndpoints = runners.Select(r => r.EndpointName).ToList();

                    PerformScenarios(runners, done);
                }
                finally
                {
                    Parallel.ForEach(runners, runner => AppDomain.Unload(runner.AppDomain));
                }

                runTimer.Stop();

                Parallel.ForEach(runners, runner =>
                    {
                        if (runner.BehaviourContext == null)
                            return;

                        shoulds.Where(s => s.ContextType == runner.BehaviourContext.GetType()).ToList()
                               .ForEach(v => v.Verify(runner.BehaviourContext));
                    });
            }
            catch (Exception ex)
            {
                runResult.Failed = true;
                runResult.Exception = ex;
            }

            runResult.TotalTime = runTimer.Elapsed;

            return runResult;
        }

        static IDictionary<Type, string> CreateRoutingTable(RunDescriptor runDescriptor, IEnumerable<BehaviorDescriptor> behaviorDescriptors)
        {
            var routingTable = new Dictionary<Type, string>();

            foreach (var behaviorDescriptor in behaviorDescriptors)
            {
                routingTable[behaviorDescriptor.EndpointBuilderType] = GetEndpointNameForRun(runDescriptor, behaviorDescriptor);
            }

            return routingTable;
        }

        private static void PrintSettings(IEnumerable<KeyValuePair<string, string>> settings)
        {
            Console.WriteLine("");
            Console.WriteLine("Using settings:");
            foreach (KeyValuePair<string, string> pair in settings)
            {
                Console.Out.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        static void PerformScenarios(IEnumerable<ActiveRunner> runners, Func<BehaviorContext, bool> done)
        {
            var endpoints = runners.Select(r => r.Instance).ToList();

            //hack until we move the context to the scenario level
            var context = runners.FirstOrDefault(r => r.BehaviourContext != null).BehaviourContext;

            StartEndpoints(endpoints);

            var startTime = DateTime.UtcNow;
            var maxTime = TimeSpan.FromSeconds(60);

            Task.WaitAll(endpoints.Select(endpoint => Task.Factory.StartNew(() => SpinWait.SpinUntil(() => endpoint.Done() || done(context), maxTime))).Cast<Task>().ToArray());

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
            endpoints.ForEach(e => sb.AppendLine(string.Format("{0} - {1}", e.Name(), e.Done() ? "Done" : "Not done")));

            return sb.ToString();
        }

        static void StartEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(() =>
                {
                    var result = endpoint.Start();

                    if (result.Failed)
                        throw new ScenarioException(string.Format("Endpoint failed to start - {0}", result.ExceptionMessage));

                    endpoint.ApplyWhens();

                })).ToArray();

            Task.WaitAll(tasks);
        }

        static void StopEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(() =>
                {
                    var result = endpoint.Stop();
                    if (result.Failed)
                        throw new ScenarioException(string.Format("Endpoint failed to stop - {0}", result.ExceptionMessage));

                })).ToArray();

            Task.WaitAll(tasks);
        }

        static List<ActiveRunner> InitializeRunners(RunDescriptor runDescriptor, IList<BehaviorDescriptor> behaviorDescriptors)
        {
            var runners = new List<ActiveRunner>();
            var routingTable = CreateRoutingTable(runDescriptor, behaviorDescriptors);

            foreach (var behaviorDescriptor in behaviorDescriptors)
            {
                var endpointName = GetEndpointNameForRun(runDescriptor, behaviorDescriptor);


                var runner = PrepareRunner(endpointName);
                runner.BehaviourContext = behaviorDescriptor.CreateContext();
                var result = runner.Instance.Initialize(runDescriptor, behaviorDescriptor.EndpointBuilderType,
                                                        routingTable, endpointName, runner.BehaviourContext);


                if (result.Failed)
                    throw new ScenarioException(string.Format("Endpoint {0} failed to initialize - {1}", runner.Instance.Name(), result.ExceptionMessage));

                runners.Add(runner);
            }

            return runners;
        }

        static string GetEndpointNameForRun(RunDescriptor runDescriptor, BehaviorDescriptor behaviorDescriptor)
        {
            var endpointName = Conventions.EndpointNamingConvention(behaviorDescriptor.EndpointBuilderType) + "." + runDescriptor.Key + "." +
                               runDescriptor.Permutation;
            return endpointName;
        }

        static ActiveRunner PrepareRunner(string endpointName)
        {
            var domainSetup = new AppDomainSetup
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    LoaderOptimization = LoaderOptimization.SingleDomain
                };

            var appDomain = AppDomain.CreateDomain(endpointName, AppDomain.CurrentDomain.Evidence, domainSetup);

            return new ActiveRunner
                {
                    Instance = (EndpointRunner)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(EndpointRunner).FullName),
                    AppDomain = appDomain,
                    EndpointName = endpointName
                };
        }
    }

    public class RunResult
    {
        public bool Failed { get; set; }

        public Exception Exception { get; set; }

        public TimeSpan TotalTime { get; set; }

        public IEnumerable<string> ActiveEndpoints
        {
            get
            {
                if (activeEndpoints == null)
                    activeEndpoints = new List<string>();

                return activeEndpoints;
            }
            set { activeEndpoints = value.ToList(); }
        }

        IList<string> activeEndpoints;
    }
}