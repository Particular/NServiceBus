
// ReSharper disable UnusedParameter.Global
namespace NServiceBus
{
    using System;

    public static partial class ConfigureQueueCreation
    {

        /// <summary>
        /// If queues configured do not exist, will cause them not to be created on startup.
        /// </summary>
        [ObsoleteEx(Replacement = "Configure.With(c=>.DoNotCreateQueues())", RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static Configure DoNotCreateQueues(this Configure config)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets whether or not queues should be created
        /// </summary>
        [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0")]
        public static bool DontCreateQueues
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}