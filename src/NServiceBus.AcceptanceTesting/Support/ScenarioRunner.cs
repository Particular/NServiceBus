namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Customization;

    public class ScenarioRunner
    {
        public static async Task Run(List<RunDescriptor> runDescriptors, List<EndpointBehavior> behaviorDescriptors, List<IScenarioVerification> shoulds, Func<ScenarioContext, bool> done, Action<RunSummary> reports)
        {
            var totalRuns = runDescriptors.Count;

            var results = new ConcurrentBag<RunSummary>();

            try
            {
                foreach (var runDescriptor in runDescriptors)
                {
                    Console.WriteLine("{0} - Started @ {1}", runDescriptor.Key, DateTime.Now.ToString(CultureInfo.InvariantCulture));

                    ContextAppenderFactory.SetContext(runDescriptor.ScenarioContext);
                    var runResult = await PerformTestRun(behaviorDescriptors, shoulds, runDescriptor, done).ConfigureAwait(false);
                    ContextAppenderFactory.SetContext(null);

                    Console.WriteLine("{0} - Finished @ {1}", runDescriptor.Key, DateTime.Now.ToString(CultureInfo.InvariantCulture));

                    results.Add(new RunSummary
                    {
                        Result = runResult,
                        RunDescriptor = runDescriptor,
                        Endpoints = behaviorDescriptors
                    });

                    if (runResult.Failed)
                    {
                        break;
                    }
                }
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

            if (failedRuns.Count == 1)
            {
                throw failedRuns[0].Result.Exception;
            }

            if (failedRuns.Count > 1)
            {
                throw new AggregateException("Test run failed due to multiple exceptions", failedRuns.Select(f => f.Result.Exception)).Flatten();
            }

            foreach (var runSummary in results.Where(s => !s.Result.Failed))
            {
                DisplayRunResult(runSummary, totalRuns);

                reports?.Invoke(runSummary);
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

        static async Task<RunResult> PerformTestRun(List<EndpointBehavior> behaviorDescriptors, List<IScenarioVerification> shoulds, RunDescriptor runDescriptor, Func<ScenarioContext, bool> done)
        {
            var runResult = new RunResult
            {
                ScenarioContext = runDescriptor.ScenarioContext
            };

            var runTimer = new Stopwatch();
            runTimer.Start();

            try
            {
                var endpoints = await InitializeRunners(runDescriptor, behaviorDescriptors).ConfigureAwait(false);

                runResult.ActiveEndpoints = endpoints.Select(r => r.EndpointName).ToList();

                await PerformScenarios(runDescriptor, endpoints, () => done(runDescriptor.ScenarioContext)).ConfigureAwait(false);

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

        static void PrintSettings(IEnumerable<KeyValuePair<string, object>> settings)
        {
            Console.WriteLine();
            Console.WriteLine("Using settings:");
            foreach (var pair in settings)
            {
                Console.WriteLine("   {0}: {1}", pair.Key, pair.Value);
            }
            Console.WriteLine();
        }

        static async Task PerformScenarios(RunDescriptor runDescriptor, IEnumerable<ActiveRunner> runners, Func<bool> done)
        {
            using (var cts = new CancellationTokenSource())
            {
                var endpoints = runners.Select(r => r.Instance).ToList();

                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                try
                {
                    await StartEndpoints(endpoints, cts).ConfigureAwait(false);
                    runDescriptor.ScenarioContext.EndpointsStarted = true;
                    await ExecuteWhens(endpoints, cts).ConfigureAwait(false);

                    var startTime = DateTime.UtcNow;
                    var maxTime = runDescriptor.Settings.TestExecutionTimeout ?? TimeSpan.FromSeconds(90);
                    while (!done() && !cts.Token.IsCancellationRequested)
                    {
                        if (DateTime.UtcNow - startTime > maxTime)
                        {
                            ThrowOnFailedMessages(runDescriptor, endpoints);
                            throw new TimeoutException(GenerateTestTimedOutMessage(maxTime));
                        }

                        await Task.Delay(1).ConfigureAwait(false);
                    }

                    startTime = DateTime.UtcNow;
                    var unfinishedFailedMessagesMaxWaitTime = TimeSpan.FromSeconds(30);
                    while (runDescriptor.ScenarioContext.UnfinishedFailedMessages.Values.Any(x => x))
                    {
                        if (DateTime.UtcNow - startTime > unfinishedFailedMessagesMaxWaitTime)
                        {
                            throw new Exception("Some failed messages were not handled by the recoverability feature.");
                        }

                        await Task.Delay(1).ConfigureAwait(false);
                    }
                }
                finally
                {
                    await StopEndpoints(endpoints).ConfigureAwait(false);
                }

                ThrowOnFailedMessages(runDescriptor, endpoints);
            }
        }

        static void ThrowOnFailedMessages(RunDescriptor runDescriptor, List<EndpointRunner> endpoints)
        {
            var unexpectedFailedMessages = runDescriptor.ScenarioContext.FailedMessages
                .Where(kvp => endpoints.Single(e => e.Name() == kvp.Key).FailOnErrorMessage)
                .SelectMany(kvp => kvp.Value)
                .ToList();

            if (unexpectedFailedMessages.Any())
            {
                throw new MessagesFailedException(unexpectedFailedMessages, runDescriptor.ScenarioContext);
            }
        }

        static string GenerateTestTimedOutMessage(TimeSpan maxTime)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"The maximum time limit for this test({maxTime.TotalSeconds}s) has been reached");
            sb.AppendLine("----------------------------------------------------------------------------");

            return sb.ToString();
        }

        static Task StartEndpoints(IEnumerable<EndpointRunner> endpoints, CancellationTokenSource cts)
        {
            var startTimeout = TimeSpan.FromMinutes(2);
            return endpoints.Select(endpoint => StartEndpoint(endpoint, cts))
                .ExecuteWithinTimeout(startTimeout, new Exception($"Starting endpoints took longer than {startTimeout.TotalMinutes} minutes."));
        }

        static async Task StartEndpoint(EndpointRunner endpoint, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await endpoint.Start(token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                cts.Cancel();
                Console.WriteLine($"Endpoint {endpoint.Name()} failed to start.");
                throw;
            }
        }

        static Task ExecuteWhens(IEnumerable<EndpointRunner> endpoints, CancellationTokenSource cts)
        {
            var whenTimeout = TimeSpan.FromSeconds(60);
            return endpoints.Select(endpoint => ExecuteWhens(endpoint, cts))
                .ExecuteWithinTimeout(whenTimeout, new Exception($"Executing given and whens took longer than {whenTimeout.TotalSeconds} seconds."));
        }

        static async Task ExecuteWhens(EndpointRunner endpoint, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await endpoint.Whens(token).ConfigureAwait(false);
            }
            catch (Exception)
            {
                cts.Cancel();
                Console.WriteLine($"Whens for endpoint {endpoint.Name()} failed to execute.");
                throw;
            }
        }

        static Task StopEndpoints(IEnumerable<EndpointRunner> endpoints)
        {
            var stopTimeout = TimeSpan.FromMinutes(2);
            return endpoints.Select(async endpoint =>
            {
                Console.WriteLine("Stopping endpoint: {0}", endpoint.Name());
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await endpoint.Stop().ConfigureAwait(false);
                    stopwatch.Stop();
                    Console.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name(), stopwatch.Elapsed);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Endpoint {endpoint.Name()} failed to stop.");
                    throw;
                }
            }).ExecuteWithinTimeout(stopTimeout, new Exception($"Stopping endpoints took longer than {stopTimeout.TotalMinutes} minutes."));
        }

        static async Task<ActiveRunner[]> InitializeRunners(RunDescriptor runDescriptor, List<EndpointBehavior> endpointBehaviors)
        {
            var routingTable = CreateRoutingTable(endpointBehaviors);

            var runnerInitializations = endpointBehaviors.Select(async endpointBehavior =>
            {
                var endpointName = GetEndpointNameForRun(endpointBehavior);

                if (endpointName.Length > 77)
                {
                    throw new Exception($"Endpoint name '{endpointName}' is larger than 77 characters and will cause issues with MSMQ queue names. Rename the test class or endpoint.");
                }

                var runner = new ActiveRunner
                {
                    Instance = new EndpointRunner(),
                    EndpointName = endpointName
                };

                try
                {
                    await runner.Instance.Initialize(runDescriptor, endpointBehavior, routingTable, endpointName).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Endpoint {runner.Instance.Name()} failed to initialize");
                    throw;
                }

                return runner;
            });

            try
            {
                var x = await Task.WhenAll(runnerInitializations).ConfigureAwait(false);
                return x;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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
                {
                    activeEndpoints = new List<string>();
                }

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