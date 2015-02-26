using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;

namespace NServiceBus.AcceptanceTesting.Support
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
    using Customization;

    public class ScenarioRunner
    {
        public static void Run(IList<RunDescriptor> runDescriptors, IList<EndpointBehavior> behaviorDescriptors, IList<IScenarioVerification> shoulds, Func<ScenarioContext, bool> done, int limitTestParallelismTo, Action<RunSummary> reports, Func<Exception, bool> allowedExceptions)
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
                Console.WriteLine("Parallelism limited to: {0}", maxParallelism);

                po.MaxDegreeOfParallelism = maxParallelism;
            }

            if (limitTestParallelismTo > 0)
            {
                po.MaxDegreeOfParallelism = limitTestParallelismTo;
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

                    Console.WriteLine("{0} - Started @ {1}", runDescriptor.Key, DateTime.Now.ToString());

                    var runResult = PerformTestRun(behaviorDescriptors, shoulds, runDescriptor, done, allowedExceptions);

                    Console.WriteLine("{0} - Finished @ {1}", runDescriptor.Key, DateTime.Now.ToString());

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
                Console.WriteLine("Test run aborted due to test failures");
            }

            var failedRuns = results.Where(s => s.Result.Failed).ToList();

            foreach (var runSummary in failedRuns)
            {
                DisplayRunResult(runSummary, totalRuns);
            }

            if (failedRuns.Any())
            {
                throw new AggregateException("Test run failed due to one or more exception", failedRuns.Select(f => f.Result.Exception));
            }

            foreach (var runSummary in results.Where(s => !s.Result.Failed))
            {
                DisplayRunResult(runSummary, totalRuns);

                if (reports != null)
                {
                    reports(runSummary);
                }
            }
        }

        static void DisplayRunResult(RunSummary summary, int totalRuns)
        {
            var runDescriptor = summary.RunDescriptor;
            var runResult = summary.Result;

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Test summary for: {0}", runDescriptor.Key);
            if (totalRuns > 1)
            {
                Console.WriteLine(" - Permutation: {0}({1})", runDescriptor.Permutation, totalRuns);
            }
            Console.WriteLine();

            PrintSettings(runDescriptor.Settings);

            Console.WriteLine();
            Console.WriteLine("Endpoints:");

            foreach (var endpoint in runResult.ActiveEndpoints)
            {
                Console.WriteLine("     - {0}", endpoint);
            }

            if (runResult.Failed)
            {
                Console.WriteLine("Test failed: {0}", runResult.Exception);
            }
            else
            {
                Console.WriteLine("Result: Successful - Duration: {0}", runResult.TotalTime);
            }

            //dump trace and context regardless since asserts outside the should could still fail the test
            Console.WriteLine();
            Console.WriteLine("Context:");

            foreach (var prop in runResult.ScenarioContext.GetType().GetProperties())
            {
                if (prop.Name == "Trace")
                {
                    continue;
                }

                Console.WriteLine("{0} = {1}", prop.Name, prop.GetValue(runResult.ScenarioContext, null));
            }

            Console.WriteLine();
            Console.WriteLine("Trace:");
            Console.WriteLine(runResult.ScenarioContext.Trace);
            Console.WriteLine("------------------------------------------------------");
        }

        static RunResult PerformTestRun(IList<EndpointBehavior> behaviorDescriptors, IList<IScenarioVerification> shoulds, RunDescriptor runDescriptor, Func<ScenarioContext, bool> done, Func<Exception, bool> allowedExceptions)
        {
            var runResult = new RunResult
            {
                ScenarioContext = runDescriptor.ScenarioContext
            };

            var runTimer = new Stopwatch();

            runTimer.Start();

            try
            {
                var runners = InitializeRunners(runDescriptor, behaviorDescriptors);

                try
                {
                    runResult.ActiveEndpoints = runners.Select(r => r.EndpointName).ToList();

                    PerformScenarios(runDescriptor, runners, () =>
                    {
                        if (!string.IsNullOrEmpty(runDescriptor.ScenarioContext.Exceptions))
                        {
                            var ex = new Exception(runDescriptor.ScenarioContext.Exceptions);
                            if (!allowedExceptions(ex))
                            {
                                throw new Exception("Failures in endpoints");
                            }
                        }
                        return done(runDescriptor.ScenarioContext);
                    });
                }
                finally
                {
                    if (runDescriptor.UseSeparateAppdomains)
                    {
                        UnloadAppDomains(runners);
                    }
                }

                runTimer.Stop();

                Parallel.ForEach(runners, runner =>
                {
                    foreach (var v in shoulds.Where(s => s.ContextType == runDescriptor.ScenarioContext.GetType()))
                    {
                        v.Verify(runDescriptor.ScenarioContext);
                    }
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

        static void UnloadAppDomains(IEnumerable<ActiveRunner> runners)
        {
            Parallel.ForEach(runners, runner =>
            {
                try
                {
                    AppDomain.Unload(runner.AppDomain);
                }
                catch (CannotUnloadAppDomainException ex)
                {
                    Console.WriteLine("Failed to unload appdomain {0}, reason: {1}", runner.AppDomain.FriendlyName, ex.ToString());
                }
            });
        }

        static IDictionary<Type, string> CreateRoutingTable(IEnumerable<EndpointBehavior> behaviorDescriptors)
        {
            var routingTable = new Dictionary<Type, string>();

            foreach (var behaviorDescriptor in behaviorDescriptors)
            {
                routingTable[behaviorDescriptor.EndpointBuilderType] = GetEndpointNameForRun(behaviorDescriptor);
            }

            return routingTable;
        }

        private static void PrintSettings(IEnumerable<KeyValuePair<string, string>> settings)
        {
            Console.WriteLine();
            Console.WriteLine("Using settings:");
            foreach (var pair in settings)
            {
                Console.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        static void PerformScenarios(RunDescriptor runDescriptor, IEnumerable<ActiveRunner> runners, Func<bool> done)
        {
            var endpoints = runners.Select(r => r.Instance).ToList();

            StartEndpoints(endpoints);

            runDescriptor.ScenarioContext.EndpointsStarted = true;

            var startTime = DateTime.UtcNow;
            var maxTime = runDescriptor.TestExecutionTimeout;

            try
            {
                Task.WaitAll(endpoints.Select(endpoint => Task.Factory.StartNew(() => SpinWait.SpinUntil(done, maxTime))).Cast<Task>().ToArray(), maxTime);

                if ((DateTime.UtcNow - startTime) > maxTime)
                {
                    throw new ScenarioException(GenerateTestTimedOutMessage(maxTime));
                }
            }
            finally
            {
                StopEndpoints(endpoints);
            }
        }

        static string GenerateTestTimedOutMessage(TimeSpan maxTime)
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("The maximum time limit for this test({0}s) has been reached",
                                        maxTime.TotalSeconds));
            sb.AppendLine("----------------------------------------------------------------------------");

            return sb.ToString();
        }

        static void StartEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(() =>
            {
                var result = endpoint.Start();

                if (result.Failed)
                    throw new ScenarioException("Endpoint failed to start", result.Exception);
            })).ToArray();

            if (!Task.WaitAll(tasks, TimeSpan.FromMinutes(2)))
                throw new Exception("Starting endpoints took longer than 2 minutes");
        }

        static void StopEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Factory.StartNew(() =>
            {
                Console.WriteLine("Stopping endpoint: {0}", endpoint.Name());
                var sw = new Stopwatch();
                sw.Start();
                var result = endpoint.Stop();
                sw.Stop();
                if (result.Failed)
                {
                    throw new ScenarioException("Endpoint failed to stop", result.Exception);
                }

                Console.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name(), sw.Elapsed);
            })).ToArray();

            if (!Task.WaitAll(tasks, TimeSpan.FromMinutes(2)))
            {
                throw new Exception("Stopping endpoints took longer than 2 minutes");
            }
        }

        static List<ActiveRunner> InitializeRunners(RunDescriptor runDescriptor, IList<EndpointBehavior> behaviorDescriptors)
        {
            var runners = new List<ActiveRunner>();
            var routingTable = CreateRoutingTable(behaviorDescriptors);

            foreach (var behaviorDescriptor in behaviorDescriptors)
            {
                var endpointName = GetEndpointNameForRun(behaviorDescriptor);

                if (endpointName.Length > 77)
                {
                    throw new Exception(string.Format("Endpoint name '{0}' is larger than 77 characters and will cause issues with MSMQ queue names. Please rename your test class or endpoint!",endpointName));
                }

                var runner = PrepareRunner(endpointName, behaviorDescriptor.AppConfig, runDescriptor.UseSeparateAppdomains);
                var result = runner.Instance.Initialize(runDescriptor, behaviorDescriptor, routingTable, endpointName);

                if (runDescriptor.UseSeparateAppdomains)
                {
                    // Extend the lease to the timeout value specified.
                    var serverLease = (ILease)RemotingServices.GetLifetimeService(runner.Instance);

                    // Add the execution time + additional time for the endpoints to be able to stop gracefully
                    var totalLifeTime = runDescriptor.TestExecutionTimeout.Add(TimeSpan.FromMinutes(2));
                    serverLease.Renew(totalLifeTime);
                }

                if (result.Failed)
                {
                    throw new ScenarioException(string.Format("Endpoint {0} failed to initialize", runner.Instance.Name()), result.Exception);
                }

                runners.Add(runner);
            }

            return runners;
        }

        static string GetEndpointNameForRun(EndpointBehavior endpointBehavior)
        {
            return Conventions.EndpointNamingConvention(endpointBehavior.EndpointBuilderType);
        }

        static ActiveRunner PrepareRunner(string endpointName, string appConfigPath, bool useSeparateAppdomains)
        {
            if (!useSeparateAppdomains)
            {
                return new ActiveRunner
                {
                    Instance = new EndpointRunner(),
                    EndpointName = endpointName
                };
            }
            var domainSetup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                LoaderOptimization = LoaderOptimization.SingleDomain,
                ConfigurationFile = appConfigPath ?? AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
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

        public ScenarioContext ScenarioContext { get; set; }

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

    public class RunSummary
    {
        public RunResult Result { get; set; }

        public RunDescriptor RunDescriptor { get; set; }

        public IEnumerable<EndpointBehavior> Endpoints { get; set; }
    }
}