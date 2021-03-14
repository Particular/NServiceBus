namespace NServiceBus.Features
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;
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
        protected internal override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
        {
            if (!context.Settings.TryGet("FileShareDataBusPath", out string basePath))
            {
                throw new InvalidOperationException("Specify the basepath for FileShareDataBus, eg endpointConfiguration.UseDataBus<FileShareDataBus>().BasePath(\"c:\\databus\")");
            }
            var dataBus = new FileShareDataBusImplementation(basePath);

            context.Container.AddSingleton(typeof(IDataBus), dataBus);

            return Task.CompletedTask;
        }
    }
}