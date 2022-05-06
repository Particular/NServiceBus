namespace NServiceBus
{
    using System;
    using Pipeline;

    static class PipelineStatisticsExtensions
    {
        const string ExecutionStartTimeSettingsKey = "NServiceBus.PipelineStartTime";

        public static void SetPipelineStartTime(this RootContext rootContext, DateTimeOffset startTime) => rootContext.Extensions.Set(ExecutionStartTimeSettingsKey, startTime);

        public static DateTimeOffset? GetPipelineStartTime(this IBehaviorContext behaviorContext)
        {
            if (behaviorContext.Extensions.TryGet(ExecutionStartTimeSettingsKey, out DateTimeOffset startTime))
            {
                return startTime;
            }

            return null;
        }
    }
}