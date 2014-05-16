namespace NServiceBus.Pipeline
{
    using System;

    public class ReplaceBehavior
    {
        public string ReplaceId { get; set; }
        public string Description { get; set; }
        public Type BehaviorType { get; set; }
    }
}