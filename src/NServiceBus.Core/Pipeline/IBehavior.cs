namespace NServiceBus.Pipeline
{
    using System.ComponentModel;

    // hide for now until we have confirmed the API
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public interface IBehavior
    {
        IBehavior Next { get; set; }
        void Invoke(IBehaviorContext context);
    }
}