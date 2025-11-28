#nullable enable

namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public class RunDescriptor(ScenarioContext context, IServiceCollection services)
{
    public RunSettings Settings { get; } = new();

    public ScenarioContext ScenarioContext { get; } = context;

    /// <summary>
    /// TODO This is not thread safe
    /// </summary>
    public IServiceCollection Services { get; } = services;

    public ServiceProvider? ServiceProvider { get; set; }

    public void OnTestCompleted(Func<RunSummary, Task> testCompletedCallback) => onCompletedCallbacks?.Add(testCompletedCallback);

    internal async Task RaiseOnTestCompleted(RunSummary result)
    {
        if (onCompletedCallbacks is not null)
        {
            await Task.WhenAll(onCompletedCallbacks.Select(c => c(result))).ConfigureAwait(false);
        }
        onCompletedCallbacks = null;
    }

    internal List<Func<RunSummary, Task>>? onCompletedCallbacks = [];
}