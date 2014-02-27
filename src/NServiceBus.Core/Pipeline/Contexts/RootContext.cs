namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.ComponentModel;
    using ObjectBuilder;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RootContext : BehaviorContext
    {
        public RootContext(IBuilder builder) : base(null)
        {
            Set(builder);
        }
    }
}