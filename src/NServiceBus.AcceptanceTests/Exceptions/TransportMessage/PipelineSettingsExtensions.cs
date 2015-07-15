namespace NServiceBus.AcceptanceTests.Exceptions
{
    using NServiceBus.Pipeline;

    static class PipelineSettingsExtensions
    {
        public static void RegistBehaviorsWhichCorruptTheStandardSerializerAndRestoreItAfterwards(this PipelineSettings settings)
        {
            settings.Register<CorruptSerializer.Registration>();
            settings.Register<UncorruptSerializer.Registration>();
        }
    }
}