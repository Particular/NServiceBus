namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class RunDescriptor(ScenarioContext context, HostApplicationBuilder builder)
{
    public RunSettings Settings { get; } = new();

    public ScenarioContext ScenarioContext => context;

    public IServiceCollection Services => Builder.Services;
    public HostApplicationBuilder Builder { get; } = builder;

    public IServiceProvider? ServiceProvider { get; set; }

    public void OnTestCompleted(Func<RunSummary, Task> testCompletedCallback) => onCompletedCallbacks?.Add(testCompletedCallback);

    internal async Task RaiseOnTestCompleted(RunSummary result)
    {
        if (onCompletedCallbacks is not null)
        {
            await Task.WhenAll(onCompletedCallbacks.Select(c => c(result))).ConfigureAwait(false);
        }
        onCompletedCallbacks = null;
    }

    List<Func<RunSummary, Task>>? onCompletedCallbacks = [];
}