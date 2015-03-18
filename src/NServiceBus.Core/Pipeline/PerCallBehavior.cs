namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    class PerCallBehavior : BehaviorInstance
    {
        readonly IBuilder defaultBuilder;

        public PerCallBehavior(Type type, IBuilder defaultBuilder) : base(type)
        {
            this.defaultBuilder = defaultBuilder;
        }

        public override object GetInstance()
        {
            return defaultBuilder.Build(Type);
        }
    }
}