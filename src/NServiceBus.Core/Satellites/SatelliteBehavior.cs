namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using System.Threading.Tasks;

    /// <summary>
    /// A base class for satellite behaviors.
    /// </summary>
    public abstract class SatelliteBehavior: PipelineTerminator<PhysicalMessageProcessingStageBehavior.Context>
    {
    }
}