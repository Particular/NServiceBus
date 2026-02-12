#nullable enable

namespace NServiceBus;

using System.Threading.Tasks;
using Pipeline;

interface IPipeline;

/// <summary>
/// When adding new pipelines make sure to update the <see cref="PipelineInvokers"/> to include the context and stages.
/// </summary>
interface IPipeline<in TContext> : IPipeline
    where TContext : IBehaviorContext
{
    Task Invoke(TContext context);
}