﻿namespace NServiceBus.AcceptanceTesting.Support
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ScenarioRunner
    {
        public static async Task Run(RunDescriptor runDescriptor, List<IComponentBehavior> behaviorDescriptors, Func<ScenarioContext, bool> done)
        {
            Console.WriteLine("Started test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            ContextAppenderFactory.SetContext(runDescriptor.ScenarioContext);
            var runResult = await PerformTestRun(behaviorDescriptors, runDescriptor, done).ConfigureAwait(false);
            ContextAppenderFactory.SetContext(null);

            Console.WriteLine("Finished test @ {0}", DateTime.Now.ToString(CultureInfo.InvariantCulture));

            var runSummary = new RunSummary
            {
                Result = runResult,
                RunDescriptor = runDescriptor,
                Endpoints = behaviorDescriptors
            };

            DisplayRunResult(runSummary);

            if (runSummary.Result.Failed)
            {
                throw runSummary.Result.Exception;
            }
        }

        static void DisplayRunResult(RunSummary summary)
        {
            var runDescriptor = summary.RunDescriptor;
            var runResult = summary.Result;

            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("Test summary:");
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

        static async Task<RunResult> PerformTestRun(List<IComponentBehavior> behaviorDescriptors, RunDescriptor runDescriptor, Func<ScenarioContext, bool> done)
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
                runResult.Exception = ex;
            }

            runResult.TotalTime = runTimer.Elapsed;

            return runResult;
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

        static async Task PerformScenarios(RunDescriptor runDescriptor, ComponentRunner[] runners, Func<bool> done)
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
            catch (Exception)
            {
                cts.Cancel();
                Console.WriteLine($"Endpoint {component.Name} failed to start.");
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
            catch (Exception)
            {
                cts.Cancel();
                Console.WriteLine($"Whens for endpoint {component.Name} failed to execute.");
                throw;
            }
        }

        static Task StopEndpoints(IEnumerable<ComponentRunner> endpoints)
        {
            var stopTimeout = TimeSpan.FromMinutes(2);
            return endpoints.Select(async endpoint =>
            {
                await Task.Yield(); // ensure all endpoints are stopped even if a synchronous implementation throws
                Console.WriteLine("Stopping endpoint: {0}", endpoint.Name);
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    await endpoint.Stop().ConfigureAwait(false);
                    stopwatch.Stop();
                    Console.WriteLine("Endpoint: {0} stopped ({1}s)", endpoint.Name, stopwatch.Elapsed);
                }
                catch (Exception)
                {
                    Console.WriteLine($"Endpoint {endpoint.Name} failed to stop.");
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

        public IEnumerable<IComponentBehavior> Endpoints { get; set; }
    }
}