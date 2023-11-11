namespace NServiceBus.Features;

using System;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.DataBus;

class DataBusFileBased : Feature
{
    public DataBusFileBased()
    {
        DependsOn<DataBus>();
    }

    /// <summary>
    /// See <see cref="Feature.Setup" />
    /// </summary>
    protected internal override void Setup(FeatureConfigurationContext context)
    {
        if (!context.Settings.TryGet("FileShareDataBusPath", out string basePath))
        {
            throw new InvalidOperationException("Specify the basepath for FileShareDataBus, eg endpointConfiguration.UseDataBus<FileShareDataBus>().BasePath(\"c:\\databus\")");
        }
        var dataBus = new FileShareDataBusImplementation(basePath);

        context.Services.AddSingleton(typeof(IDataBus), dataBus);
    }
}