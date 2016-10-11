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

            if (failedRuns.Any())
            {
                throw new AggregateException("Test run failed due to one or more exceptions", failedRuns.Select(f => f.Result.Exception)).Flatten();
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
            var cts = new CancellationTokenSource();
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
                        throw new ScenarioException(GenerateTestTimedOutMessage(maxTime));
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
                await StopEndpoints(endpoints, runDescriptor.ScenarioContext).ConfigureAwait(false);
            }

            ThrowOnFailedMessages(runDescriptor, endpoints);
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

        static async Task StartEndpoints(IEnumerable<EndpointRunner> endpoints, CancellationTokenSource cts)
        {
            var tasks = endpoints.Select(endpoint => StartEndpoint(endpoint, cts));
            var whenAll = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
            var completedTask = await Task.WhenAny(whenAll, timeoutTask).ConfigureAwait(false);

            if (completedTask.Equals(timeoutTask))
            {
                throw new Exception("Starting endpoints took longer than 2 minutes");
            }
            await completedTask.ConfigureAwait(false);
        }

        static async Task StartEndpoint(EndpointRunner endpoint, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await endpoint.Start(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                throw new ScenarioException("Endpoint failed to start", ex);
            }
        }

        static async Task ExecuteWhens(IEnumerable<EndpointRunner> endpoints, CancellationTokenSource cts)
        {
            var tasks = endpoints.Select(endpoint => ExecuteWhens(endpoint, cts));
            var whenAll = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30));
            var completedTask = await Task.WhenAny(whenAll, timeoutTask).ConfigureAwait(false);

            if (completedTask.Equals(timeoutTask))
            {
                throw new Exception("Executing given and whens took longer than 30 seconds.");
            }

            if (completedTask.IsFaulted && completedTask.Exception != null)
            {
                ExceptionDispatchInfo.Capture(completedTask.Exception).Throw();
            }
        }

        static async Task ExecuteWhens(EndpointRunner endpoint, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await endpoint.Whens(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                throw new ScenarioException("Whens failed to execute", ex);
            }
        }

        static async Task StopEndpoints(IEnumerable<EndpointRunner> endpoints, ScenarioContext scenarioContext)
        {
            var tasks = endpoints.Select(async endpoint =>
            {
                Console.WriteLine("Stopping endpoint: {0}", endpoint.Name());
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await endpoint.Stop().ConfigureAwait(false);
                    stopwatch.Stop();
                    Console.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name(), stopwatch.Elapsed);
                }
                catch (Exception ex)
                {
                    throw new ScenarioException("Endpoint failed to stop", ex);
                }
            });

            var whenAll = Task.WhenAll(tasks);
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(2));
            var completedTask = await Task.WhenAny(whenAll, timeoutTask).ConfigureAwait(false);

            if (completedTask == timeoutTask)
            {
                throw new Exception("Stopping endpoints took longer than 2 minutes");
            }
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
                catch (Exception e)
                {
                    throw new ScenarioException($"Endpoint {runner.Instance.Name()} failed to initialize", e);
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