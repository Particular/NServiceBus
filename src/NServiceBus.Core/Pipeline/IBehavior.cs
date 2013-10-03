namespace NServiceBus.Pipeline
{
    public interface IBehavior
    {
        IBehavior Next { get; set; }
        void Invoke(IBehaviorContext context);
    }
}