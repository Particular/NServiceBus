namespace NServiceBus.Pipeline
{
    using System;
    using NServiceBus.ObjectBuilder;

    class StaticBehavior: BehaviorInstance 
    {
        readonly Lazy<object> lazyInstance; 

        public StaticBehavior(Type type, IBuilder defaultBuilder) : base(type)
        {
            lazyInstance = new Lazy<object>(() => defaultBuilder.Build(type));
        }

        public override object GetInstance()
        {
            return lazyInstance.Value;
        }
    }
}