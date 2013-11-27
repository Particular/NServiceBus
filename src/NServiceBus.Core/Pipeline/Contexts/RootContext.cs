namespace NServiceBus.Pipeline.Contexts
{
    using System.ComponentModel;
    using ObjectBuilder;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder) : base(null)
        {
            Set(builder);
        }
    }
}