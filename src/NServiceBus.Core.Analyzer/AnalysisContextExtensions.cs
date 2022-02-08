namespace NServiceBus.Core.Analyzer
{
    using Microsoft.CodeAnalysis.Diagnostics;

    static class AnalysisContextExtensions
    {
        public static AnalysisContext WithDefaultSettings(this AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            return context;
        }
    }
}
