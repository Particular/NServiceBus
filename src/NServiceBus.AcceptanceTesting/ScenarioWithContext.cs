namespace NServiceBus.AcceptanceTesting;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Support;

public class ScenarioWithContext<TContext>(Action<TContext> initializer) : IScenarioWithEndpointBehavior<TContext>
    where TContext : ScenarioContext, new()
{
    public Task<TContext> Run(TimeSpan? testExecutionTimeout)
    {
        var settings = new RunSettings();
        if (testExecutionTimeout.HasValue)
        {
            settings.TestExecutionTimeout = testExecutionTimeout.Value;
        }

        return Run(settings);
    }

    public async Task<TContext> Run(RunSettings settings)
    {
        var scenarioContext = new TContext();
        initializer(scenarioContext);

        AddScenarioContext(scenarioContext, services);

        var runDescriptor = new RunDescriptor(scenarioContext, services);
        runDescriptor.Settings.Merge(settings);

        TestExecutionContext.CurrentContext.AddRunDescriptor(runDescriptor);
        ScenarioContext.Current = scenarioContext;

        LogManager.UseFactory(Scenario.GetLoggerFactory(scenarioContext));

        kickOffTcs.SetResult(scenarioContext);
        if (doneFunc is not null)
        {
            scenarioContext.Completed = doneFunc(scenarioContext);
        }

        var sw = new Stopwatch();
        var scenarioRunner = new ScenarioRunner(runDescriptor, behaviors);

        sw.Start();
        var runSummary = await scenarioRunner.Run().ConfigureAwait(false);
        sw.Stop();

        await runDescriptor.RaiseOnTestCompleted(runSummary).ConfigureAwait(false);

        TestContext.Out.WriteLine("Test {0}: Scenario completed in {1:0.0}s", TestContext.CurrentContext.Test.FullName, sw.Elapsed.TotalSeconds);

        runSummary.Result.Exception?.Throw();

        return (TContext)runDescriptor.ScenarioContext;
    }

    public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>()
        where T : EndpointConfigurationBuilder, new()
        => WithEndpoint<T>(static _ => { });

    public IScenarioWithEndpointBehavior<TContext> WithEndpoint<T>(Action<EndpointBehaviorBuilder<TContext>> defineBehavior)
        where T : EndpointConfigurationBuilder, new()
        => WithEndpoint(new T(), defineBehavior);

    public IScenarioWithEndpointBehavior<TContext> WithEndpoint(EndpointConfigurationBuilder endpointConfigurationBuilder, Action<EndpointBehaviorBuilder<TContext>> defineBehavior)
    {
        var builder = new EndpointBehaviorBuilder<TContext>(endpointConfigurationBuilder, componentCount++);
        defineBehavior(builder);
        behaviors.Add(builder.Build());

        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> WithComponent(IComponentBehavior componentBehavior)
    {
        componentCount++;
        behaviors.Add(componentBehavior);
        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> WithServices(Action<IServiceCollection> configureServices)
    {
        behaviors.Add(new ServiceRegistrationComponent(configureServices, componentCount++));
        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> WithServiceResolve(Func<IServiceProvider, CancellationToken, Task> resolve, ServiceResolveMode resolveMode = ServiceResolveMode.BeforeStart)
    {
        behaviors.Add(new ServiceResolveComponent(resolve, componentCount++, resolveMode));
        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, bool> func)
        => Done(ctx => Task.FromResult(func(ctx)));

    public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, Task<bool>> func)
    {
        if (doneTask is not null)
        {
            throw new InvalidOperationException("Done condition has already been defined.");
        }
        doneTask = Task.Run(async () =>
        {
            // TODO proper error handling and cancellation
            var context = await kickOffTcs.Task.ConfigureAwait(false);
            while (true)
            {
                if (await func(context).ConfigureAwait(false))
                {
                    context.MarkAsCompleted();
                    break;
                }

                await Task.Delay(100).ConfigureAwait(false);
            }
        });
        return this;
    }

    public IScenarioWithEndpointBehavior<TContext> Done(Func<TContext, TaskCompletionSource>? func = null)
    {
        if (func is null)
        {
            return this;
        }

        if (doneTask is not null)
        {
            throw new InvalidOperationException("Done condition has already been defined.");
        }

        doneFunc = func;
        return this;
    }

    // static async Task<bool> Done(TContext context, Func<TContext, Task<bool>> func)
    // {
    //     var taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    //     //var startTime = DateTime.UtcNow;
    //     while (!await func(context).ConfigureAwait(false))
    //     {
    //         // if (!Debugger.IsAttached)
    //         // {
    //         //     if (DateTime.UtcNow - startTime > maxTime)
    //         //     {
    //         //         throw new TimeoutException(GenerateTestTimedOutMessage(maxTime));
    //         //     }
    //         // }
    //
    //         await Task.Delay(100).ConfigureAwait(false);
    //     }
    //     return taskCompletionSource;
    // }

    static void AddScenarioContext(TContext scenarioContext, IServiceCollection services)
    {
        var type = scenarioContext.GetType();
        while (type != typeof(object) && type is not null)
        {
            services.AddSingleton(type, scenarioContext);
            type = type.BaseType;
        }
    }

    readonly List<IComponentBehavior> behaviors = [];
    int componentCount = 0;
    Task? doneTask;
    readonly TaskCompletionSource<TContext> kickOffTcs = new(); // Deliberately not async
    readonly IServiceCollection services = new ServiceCollection();
    Func<TContext, TaskCompletionSource>? doneFunc;
}