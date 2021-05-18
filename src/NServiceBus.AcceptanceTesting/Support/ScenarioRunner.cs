namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class ScenarioRunner
    {
        public static async Task<RunSummary> Run(RunDescriptor runDescriptor, List<IComponentBehavior> behaviorDescriptors, Func<ScenarioContext, Task<bool>> done)
        {
            TestContext.WriteLine("current context: " + runDescriptor.ScenarioContext.GetType().FullName);
            TestContext.WriteLine("Started test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            var runResult = await PerformTestRun(behaviorDescriptors, runDescriptor, done).ConfigureAwait(false);

            TestContext.WriteLine("Finished test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            return new RunSummary
            {
                Result = runResult,
                RunDescriptor = runDescriptor,
                Endpoints = behaviorDescriptors
            };
        }

        static async Task<RunResult> PerformTestRun(List<IComponentBehavior> behaviorDescriptors, RunDescriptor runDescriptor, Func<ScenarioContext, Task<bool>> done)
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

                runResult.ActiveEndpoints = endpoints.Select(r => r.Name);

                await PerformScenarios(runDescriptor, endpoints, () => done(runDescriptor.ScenarioContext)).ConfigureAwait(false);

                runTimer.Stop();
            }
            catch (Exception ex)
            {
                runResult.Failed = true;
                runResult.Exception = ExceptionDispatchInfo.Capture(ex);
            }

            runResult.TotalTime = runTimer.Elapsed;

            return runResult;
        }


        static async Task PerformScenarios(RunDescriptor runDescriptor, ComponentRunner[] runners, Func<Task<bool>> done)
        {
            using (var cts = new CancellationTokenSource())
            {
                try
                {
                    await StartEndpoints(runners, cts).ConfigureAwait(false);
                    runDescriptor.ScenarioContext.EndpointsStarted = true;
                    await ExecuteWhens(runners, cts).ConfigureAwait(false);

                    var startTime = DateTime.UtcNow;
                    var maxTime = runDescriptor.Settings.TestExecutionTimeout ?? TimeSpan.FromSeconds(90);
                    while (!await done().ConfigureAwait(false) && !cts.Token.IsCancellationRequested)
                    {
                        if (!Debugger.IsAttached)
                        {
                            if (DateTime.UtcNow - startTime > maxTime)
                            {
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
            }
        }

        static string GenerateTestTimedOutMessage(TimeSpan maxTime)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"The maximum time limit for this test({maxTime.TotalSeconds}s) has been reached");
            sb.AppendLine("----------------------------------------------------------------------------");

            return sb.ToString();
        }

        static Task StartEndpoints(IEnumerable<ComponentRunner> endpoints, CancellationTokenSource cts)
        {
            var startTimeout = TimeSpan.FromMinutes(2);
            return endpoints.Select(endpoint => StartEndpoint(endpoint, cts))
                .Timebox(startTimeout, $"Starting endpoints took longer than {startTimeout.TotalMinutes} minutes.");
        }

        static async Task StartEndpoint(ComponentRunner component, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await component.Start(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                cts.Cancel();
                TestContext.WriteLine($"Endpoint {component.Name} failed to start.");
                throw;
            }
        }

        static Task ExecuteWhens(IEnumerable<ComponentRunner> endpoints, CancellationTokenSource cts)
        {
            var whenTimeout = TimeSpan.FromSeconds(60);
            return endpoints.Select(endpoint => ExecuteWhens(endpoint, cts))
                .Timebox(whenTimeout, $"Executing given and whens took longer than {whenTimeout.TotalSeconds} seconds.");
        }

        static async Task ExecuteWhens(ComponentRunner component, CancellationTokenSource cts)
        {
            var token = cts.Token;
            try
            {
                await component.ComponentsStarted(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                cts.Cancel();
                TestContext.WriteLine($"Whens for endpoint {component.Name} failed to execute.");
                throw;
            }
        }

        static Task StopEndpoints(IEnumerable<ComponentRunner> endpoints)
        {
            var stopTimeout = TimeSpan.FromMinutes(2);
            return endpoints.Select(async endpoint =>
            {
                await Task.Yield(); // ensure all endpoints are stopped even if a synchronous implementation throws
                TestContext.WriteLine("Stopping endpoint: {0}", endpoint.Name);
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await endpoint.Stop().ConfigureAwait(false);
                    stopwatch.Stop();
                    TestContext.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name, stopwatch.Elapsed);
                }
                catch (Exception)
                {
                    TestContext.WriteLine($"Endpoint {endpoint.Name} failed to stop.");
                    throw;
                }
            }).Timebox(stopTimeout, $"Stopping endpoints took longer than {stopTimeout.TotalMinutes} minutes.");
        }

        static async Task<ComponentRunner[]> InitializeRunners(RunDescriptor runDescriptor, List<IComponentBehavior> endpointBehaviors)
        {
            var runnerInitializations = endpointBehaviors.Select(endpointBehavior => endpointBehavior.CreateRunner(runDescriptor)).ToArray();
            try
            {
                var x = await Task.WhenAll(runnerInitializations).ConfigureAwait(false);
                return x;
            }
            catch (Exception e)
            {
                TestContext.WriteLine(e);
                throw;
            }
        }
    }

    public class RunResult
    {
        public bool Failed { get; set; }

        public ExceptionDispatchInfo Exception { get; set; }

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
            set => activeEndpoints = value.ToList();
        }

        IList<string> activeEndpoints;
    }

    public class RunSummary
    {
        public RunResult Result { get; set; }

        public RunDescriptor RunDescriptor { get; set; }

        public IEnumerable<IComponentBehavior> Endpoints { get; set; }
    }
}
