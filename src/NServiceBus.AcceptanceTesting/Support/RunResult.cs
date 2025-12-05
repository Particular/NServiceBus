namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

public class RunResult
{
    public bool Failed => Exception is not null;

    public ExceptionDispatchInfo? Exception { get; set; }

    public TimeSpan TotalTime { get; set; }

    public ScenarioContext ScenarioContext { get; set; } = null!;

    public IReadOnlyCollection<string> ActiveEndpoints
    {
        get
        {
            field ??= [];

            return field;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = [.. value];
        }
    }
}