namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
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
        public static async Task<RunSummary> Run(RunDescriptor runDescriptor, List<EndpointBehavior> behaviorDescriptors, Func<ScenarioContext, bool> done)
        {
            Console.WriteLine("Started test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            ContextAppenderFactory.SetContext(runDescriptor.ScenarioContext);
            var runResult = await PerformTestRun(behaviorDescriptors, runDescriptor, done).ConfigureAwait(false);
            ContextAppenderFactory.SetContext(null);

            Console.WriteLine("Finished test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            return new RunSummary
            {
                Result = runResult,
                RunDescriptor = runDescriptor,
                Endpoints = behaviorDescriptors
            };

        }



        static async Task<RunResult> PerformTestRun(List<EndpointBehavior> behaviorDescriptors, RunDescriptor runDescriptor, Func<ScenarioContext, bool> done)
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

                runResult.ActiveEndpoints = endpoints.Select(r => r.Name());

                await PerformScenarios(runDescriptor, endpoints, () => done(runDescriptor.ScenarioContext)).ConfigureAwait(false);

                runTimer.Stop();
            }
            catch (Exception ex)
            {
                runResult.Failed = true;
                runResult.Exception = ex;
            }

            runResult.TotalTime = runTimer.Elapsed;

            return runResult;
        }


        static async Task PerformScenarios(RunDescriptor runDescriptor, EndpointRunner[] runners, Func<bool> done)
        {
            using (var cts = new CancellationTokenSource())
            {
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                try
                {
                    await StartEndpoints(runners, cts).ConfigureAwait(false);
                    runDescriptor.ScenarioContext.EndpointsStarted = true;
                    await ExecuteWhens(runners, cts).ConfigureAwait(false);

                    var startTime = DateTime.UtcNow;
                    var maxTime = runDescriptor.Settings.TestExecutionTimeout ?? TimeSpan.FromSeconds(90);
                    while (!done() && !cts.Token.IsCancellationRequested)
                    {
                        if (!Debugger.IsAttached)
                        {
                            if (DateTime.UtcNow - startTime > maxTime)
                            {
                                ThrowOnFailedMessages(runDescriptor, runners);
                                throw new TimeoutException(GenerateTestTimedOutMessage(maxTime));
                            }
                        }

                        await Task.Yield();
                    }

                    startTime = DateTime.UtcNow;
                    var unfinishedFailedMessagesMaxWaitTime = TimeSpan.FromSeconds(30);
                    while (runDescriptor.ScenarioContext.UnfinishedFailedMessages.Values.Any(x => x))
                    {
                        if (DateTime.UtcNow - startTime > unfinishedFailedMessagesMaxWaitTime)
                        {
                            throw new Exception("Some failed messages were not handled by the recoverability feature.");
                        }

                        await Task.Yield();
                    }
                }
                finally
                {
                    await StopEndpoints(runners).ConfigureAwait(false);
                }

                ThrowOnFailedMessages(runDescriptor, runners);
            }
        }

        static void ThrowOnFailedMessages(RunDescriptor runDescriptor, EndpointRunner[] endpoints)
        {
            var unexpectedFailedMessages = runDescriptor.ScenarioContext.FailedMessages
                .Where(kvp => endpoints.Single(e => e.Name() == kvp.Key).FailOnErrorMessage)
                .SelectMany(kvp => kvp.Value)
                .ToList();

            if (unexpectedFailedMessages.Any())
            {
                foreach (var failedMessage in unexpectedFailedMessages)
                {
                    Console.WriteLine($"Message: {failedMessage.MessageId} failed to process and was moved to the error queue: {failedMessage.Exception}");
                }

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
                .Timebox(startTimeout, $"Starting endpoints took longer than {startTimeout.TotalMinutes} minutes.");
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
                .Timebox(whenTimeout, $"Executing given and whens took longer than {whenTimeout.TotalSeconds} seconds.");
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
            }).Timebox(stopTimeout, $"Stopping endpoints took longer than {stopTimeout.TotalMinutes} minutes.");
        }

        static async Task<EndpointRunner[]> InitializeRunners(RunDescriptor runDescriptor, List<EndpointBehavior> endpointBehaviors)
        {
            var runnerInitializations = endpointBehaviors.Select(async endpointBehavior =>
            {
                var endpointName = Conventions.EndpointNamingConvention(endpointBehavior.EndpointBuilderType);

                if (endpointName.Length > 77)
                {
                    throw new Exception($"Endpoint name '{endpointName}' is larger than 77 characters and will cause issues with MSMQ queue names. Rename the test class or endpoint.");
                }

                var runner = new EndpointRunner();

                try
                {
                    await runner.Initialize(runDescriptor, endpointBehavior, endpointName).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Endpoint {runner.Name()} failed to initialize");
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