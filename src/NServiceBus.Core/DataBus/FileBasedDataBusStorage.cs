namespace NServiceBus.Features
{
    using System;

    class FileBasedDataBusStorage : Feature, IProvideService<DataBusStorage>
    {
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            string basePath;
            if (!context.Settings.TryGet("FileShareDataBusPath", out basePath))
            {
                throw new InvalidOperationException("Specify the basepath for FileShareDataBus, eg endpointConfiguration.UseDataBus<FileShareDataBus>().BasePath(\"c:\\databus\")");
            }

            var storage = new FileShareDataBusImplementation(basePath);
            context.RegisterService(new DataBusStorage(storage));
        }
    }
}