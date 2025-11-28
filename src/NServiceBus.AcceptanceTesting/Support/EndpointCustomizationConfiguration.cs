namespace NServiceBus.AcceptanceTesting.Support;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class EndpointCustomizationConfiguration
{
    public IList<Type> TypesToInclude { get; } = [];

    public Func<RunDescriptor, Task<EndpointConfiguration>> GetConfiguration { get; set; }

    public PublisherMetadata PublisherMetadata { get; } = new();

    public string EndpointName
    {
        get => !string.IsNullOrEmpty(CustomEndpointName) ? CustomEndpointName : field;
        set;
    }

    public Type BuilderType { get; set; }

    public string CustomMachineName { get; set; }

    public string CustomEndpointName { get; set; }

    public bool DisableStartupDiagnostics { get; set; } = true;

    public bool AutoRegisterHandlers { get; set; } = true;

    public bool AutoRegisterSagas { get; set; } = true;
}