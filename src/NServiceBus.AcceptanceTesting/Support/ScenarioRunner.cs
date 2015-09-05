namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting.Customization;

    public class ScenarioRunner
    {
        public static async Task Run(IList<RunDescriptor> runDescriptors, IList<EndpointBehavior> behaviorDescriptors, IList<IScenarioVerification> shoulds, Func<ScenarioContext, bool> done, int limitTestParallelismTo, Action<RunSummary> reports, Func<Exception, bool> allowedExceptions)
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
                var runs = runDescriptors.Select(runDescriptor => Task.Run(async () =>
                {
                    if (po.CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    Console.WriteLine("{0} - Started @ {1}", runDescriptor.Key, DateTime.Now.ToString(CultureInfo.InvariantCulture));

                    var runResult = await PerformTestRun(behaviorDescriptors, shoulds, runDescriptor, done, allowedExceptions).ConfigureAwait(false);

                    Console.WriteLine("{0} - Finished @ {1}", runDescriptor.Key, DateTime.Now.ToString(CultureInfo.InvariantCulture));

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
                }, cts.Token));

                await Task.WhenAll(runs);
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

        static async Task<RunResult> PerformTestRun(IList<EndpointBehavior> behaviorDescriptors, IList<IScenarioVerification> shoulds, RunDescriptor runDescriptor, Func<ScenarioContext, bool> done, Func<Exception, bool> allowedExceptions)
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

                runResult.ActiveEndpoints = runners.Select(r => r.EndpointName).ToList();

                await PerformScenarios(runDescriptor, runners, () => done(runDescriptor.ScenarioContext), allowedExceptions).ConfigureAwait(false);

                runTimer.Stop();

                foreach (var v in shoulds.Where(s => s.ContextType == runDescriptor.ScenarioContext.GetType()))
                {
                    v.Verify(runDescriptor.ScenarioContext);
                }
            }
            catch (Exception ex)
            {
                runResult.Failed = true;
                runResult.Exception = ex;
            }

            runResult.TotalTime = runTimer.Elapsed;

            return runResult;
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

        static void PrintSettings(IEnumerable<KeyValuePair<string, string>> settings)
        {
            Console.WriteLine();
            Console.WriteLine("Using settings:");
            foreach (var pair in settings)
            {
                Console.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        static async Task PerformScenarios(RunDescriptor runDescriptor, IEnumerable<ActiveRunner> runners, Func<bool> done, Func<Exception, bool> allowedExceptions)
        {
            var endpoints = runners.Select(r => r.Instance).ToList();

            await StartEndpoints(endpoints).ConfigureAwait(false);

            runDescriptor.ScenarioContext.EndpointsStarted = true;

            var startTime = DateTime.UtcNow;
            var maxTime = runDescriptor.TestExecutionTimeout;
            
            ExceptionDispatchInfo exceptionInfo = null;
            try
            {
                // Let's use a blocking SpinWait for now
                SpinWait.SpinUntil(done, maxTime);

                if ((DateTime.UtcNow - startTime) > maxTime)
                {
                    throw new ScenarioException(GenerateTestTimedOutMessage(maxTime));
                }
            }
            catch(Exception ex)
            {
                exceptionInfo = ExceptionDispatchInfo.Capture(ex);
            }

            // With this version of C# we can't await in finally
            await StopEndpoints(endpoints);

            if (exceptionInfo != null)
            {
                exceptionInfo.Throw();
            }
            
            var exceptions = runDescriptor.ScenarioContext.Exceptions
                        .Where(ex => !allowedExceptions(ex))
                        .ToList();

            if (exceptions.Any())
            {
               throw new AggregateException(exceptions);
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

        static async Task StartEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Run(async () =>
            {
                var result = await endpoint.Start().ConfigureAwait(false);

                if (result.Failed)
                    throw new ScenarioException("Endpoint failed to start", result.Exception);
            })).ToArray();

            var whenAll = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
            var completedTask = await Task.WhenAny(whenAll, timeoutTask);

            if (completedTask.Equals(timeoutTask))
                throw new Exception("Starting endpoints took longer than 2 minutes");
        }

        static async Task StopEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var tasks = endpoints.Select(endpoint => Task.Run(async () =>
            {
                Console.WriteLine("Stopping endpoint: {0}", endpoint.Name());
                var sw = new Stopwatch();
                sw.Start();
                var result = await endpoint.Stop().ConfigureAwait(false);
                sw.Stop();
                if (result.Failed)
                {
                    throw new ScenarioException("Endpoint failed to stop", result.Exception);
                }

                Console.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name(), sw.Elapsed);
            })).ToArray();

            var whenAll = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
            var completedTask = await Task.WhenAny(whenAll, timeoutTask);

            if (completedTask.Equals(timeoutTask))
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
                    throw new Exception(string.Format("Endpoint name '{0}' is larger than 77 characters and will cause issues with MSMQ queue names. Please rename your test class or endpoint.",endpointName));
                }

                var runner = new ActiveRunner
                {
                    Instance = new EndpointRunner(),
                    EndpointName = endpointName
                };
                var result = runner.Instance.Initialize(runDescriptor, behaviorDescriptor, routingTable, endpointName);

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