namespace NServiceBus.Pipeline
{
    interface IBehavior
    {
        IBehavior Next { get; set; }
        void Invoke(IBehaviorContext context);
    }
}