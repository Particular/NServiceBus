namespace NServiceBus.Core.Analyzer.Tests.Contributors;

using Microsoft.CodeAnalysis;

// Emits the contributor from RegisterPostInitializationOutput like a third-party generator would
public sealed class ContributorFixtureGenerator : IIncrementalGenerator
{
    internal static class TrackingNames
    {
        public const string AssemblyName = nameof(AssemblyName);

        public static string[] All => [AssemblyName];
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static postInitializationContext =>
            postInitializationContext.AddSource("WorkflowContributor.g.cs", """
                                                                           using NServiceBus;

                                                                           namespace Acme.Workflows
                                                                           {
                                                                               [HandlerRegistryContributor]
                                                                               internal abstract class WorkflowContributor : IHandlerRegistryContributor
                                                                               {
                                                                                   public static void AddHandlers(EndpointConfiguration configuration) { }
                                                                                   public static void AddSagas(EndpointConfiguration configuration) { }
                                                                               }
                                                                           }
                                                                           """));

        context.RegisterSourceOutput(
            context.CompilationProvider.Select(static (compilation, _) => compilation.AssemblyName).WithTrackingName(TrackingNames.AssemblyName),
            static (_, _) => { });
    }
}
