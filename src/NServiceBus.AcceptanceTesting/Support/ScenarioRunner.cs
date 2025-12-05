namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class ScenarioRunner(
    RunDescriptor runDescriptor,
    List<IComponentBehavior> behaviorDescriptors,
    Func<ScenarioContext, Task<bool>> done)
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Code", "PS0023:Use DateTime.UtcNow or DateTimeOffset.UtcNow", Justification = "Test logging")]
    public async Task<RunSummary> Run()
    {
        runDescriptor.ScenarioContext.AddTrace("current context: " + runDescriptor.ScenarioContext.GetType().FullName);
        runDescriptor.ScenarioContext.AddTrace("Started test @ " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

        var runResult = await PerformTestRun().ConfigureAwait(false);

        runDescriptor.ScenarioContext.AddTrace("Finished test @ " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

        return new RunSummary
        {
            Result = runResult,
            RunDescriptor = runDescriptor,
            Endpoints = behaviorDescriptors
        };
    }

    async Task<RunResult> PerformTestRun()
    {
        var runResult = new RunResult
        {
            ScenarioContext = runDescriptor.ScenarioContext
        };

        var runTimer = new Stopwatch();
        runTimer.Start();

        try
        {
            var endpoints = await InitializeRunners().ConfigureAwait(false);

            runResult.ActiveEndpoints = [.. endpoints.Select(r => r.Name)];

            runDescriptor.ServiceProvider = runDescriptor.Services.BuildServiceProvider(runDescriptor.Settings.Get<ServiceProviderOptions>());

            await PerformScenarios(endpoints).ConfigureAwait(false);

            runTimer.Stop();
        }
        catch (Exception ex)
        {
            runResult.Exception = ExceptionDispatchInfo.Capture(ex);
        }

        runResult.TotalTime = runTimer.Elapsed;

        return runResult;
    }


    async Task PerformScenarios(ComponentRunner[] runners)
    {
        try
        {
            await StartEndpoints(runners).ConfigureAwait(false);
            runDescriptor.ScenarioContext.EndpointsStarted = true;
            await ExecuteWhens(runners).ConfigureAwait(false);

            var startTime = DateTime.UtcNow;
            var maxTime = runDescriptor.Settings.TestExecutionTimeout ?? TimeSpan.FromSeconds(90);
            while (!await done(runDescriptor.ScenarioContext).ConfigureAwait(false))
            {
                if (!Debugger.IsAttached)
                {
                    if (DateTime.UtcNow - startTime > maxTime)
                    {
                        throw new TimeoutException(GenerateTestTimedOutMessage(maxTime));
                    }
                }

                await Task.Delay(100).ConfigureAwait(false);
            }

            startTime = DateTime.UtcNow;
            var unfinishedFailedMessagesMaxWaitTime = TimeSpan.FromSeconds(30);
            while (runDescriptor.ScenarioContext.UnfinishedFailedMessages.Values.Any(x => x))
            {
                if (DateTime.UtcNow - startTime > unfinishedFailedMessagesMaxWaitTime)
                {
                    throw new Exception("Some failed messages were not handled by the recoverability feature.");
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        }
        finally
        {
            await StopEndpoints(runners).ConfigureAwait(false);
        }
    }

    static string GenerateTestTimedOutMessage(TimeSpan maxTime)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"The maximum time limit for this test({maxTime.TotalSeconds}s) has been reached");
        sb.AppendLine("----------------------------------------------------------------------------");

        return sb.ToString();
    }

    async Task StartEndpoints(IEnumerable<ComponentRunner> endpoints)
    {
        using var allEndpointsStartTimeout = CreateCancellationTokenSource(TimeSpan.FromMinutes(2));
        // separate (linked) CTS as otherwise a failure during endpoint startup will cause WaitAsync to throw an OperationCanceledException and hide the original error
        using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(allEndpointsStartTimeout.Token);

        await Task.WhenAll(endpoints.Select(endpoint => StartEndpoint(endpoint, combinedSource)))
            .WaitAsync(allEndpointsStartTimeout.Token)
            .ConfigureAwait(false);
    }

    async Task StartEndpoint(ComponentRunner component, CancellationTokenSource cts)
    {
        var token = cts.Token;
        try
        {
            await component.Start(token).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(token))
        {
            // signal other endpoints to stop the startup process
            await cts.CancelAsync().ConfigureAwait(false);
            runDescriptor.ScenarioContext.AddTrace($"Endpoint {component.Name} failed to start: " + ex);
            throw;
        }
    }

    async Task ExecuteWhens(IEnumerable<ComponentRunner> endpoints)
    {
        using var allWhensTimeout = CreateCancellationTokenSource(TimeSpan.FromMinutes(1));
        // separate (linked) CTS as otherwise a failure during 'When' blocks will cause WaitAsync to throw an OperationCanceledException and hide the original error
        using var combinedSource = CancellationTokenSource.CreateLinkedTokenSource(allWhensTimeout.Token);

        await Task.WhenAll(endpoints.Select(endpoint => ExecuteWhens(endpoint, combinedSource)))
            .WaitAsync(allWhensTimeout.Token)
            .ConfigureAwait(false);
    }

    async Task ExecuteWhens(ComponentRunner component, CancellationTokenSource cts)
    {
        var token = cts.Token;
        try
        {
            await component.ComponentsStarted(token).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ex.IsCausedBy(token))
        {
            // signal other endpoints to stop evaluating the when conditions
            await cts.CancelAsync().ConfigureAwait(false);
            runDescriptor.ScenarioContext.AddTrace($"Whens for endpoint {component.Name} failed to execute." + ex);
            throw;
        }
    }

    async Task StopEndpoints(IEnumerable<ComponentRunner> endpoints)
    {
        using var stopTimeoutCts = CreateCancellationTokenSource(TimeSpan.FromMinutes(2));

        try
        {
            await Task.WhenAll(endpoints.Select(endpoint => StopEndpoint(endpoint, stopTimeoutCts.Token)))
                .WaitAsync(stopTimeoutCts.Token)
                .ConfigureAwait(false);
        }
        finally
        {
            if (runDescriptor.ServiceProvider is not null)
            {
                await runDescriptor.ServiceProvider.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    async Task StopEndpoint(ComponentRunner endpoint, CancellationToken cancellationToken)
    {
        runDescriptor.ScenarioContext.AddTrace($"Stopping endpoint: {endpoint.Name}");
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await endpoint.Stop(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            runDescriptor.ScenarioContext.AddTrace($"Endpoint: {endpoint.Name} stopped ({stopwatch.Elapsed}s)");
        }
        catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
        {
            runDescriptor.ScenarioContext.AddTrace($"Endpoint {endpoint.Name} failed to stop: " + ex);
            throw;
        }
    }

    async Task<ComponentRunner[]> InitializeRunners()
    {
        var runnerInitializations = behaviorDescriptors.Select(endpointBehavior => endpointBehavior.CreateRunner(runDescriptor)).ToArray();
        return await Task.WhenAll(runnerInitializations).ConfigureAwait(false);
    }

    static CancellationTokenSource CreateCancellationTokenSource(TimeSpan timeout)
    {
        if (Debugger.IsAttached)
        {
            timeout = Timeout.InfiniteTimeSpan;
        }

        return new CancellationTokenSource(timeout);
    }
}