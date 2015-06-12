namespace NServiceBus.Pipeline
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Unicast.Transport;

    [DebuggerDisplay("{Type.Name}")]
    class BehaviorInstance
    {
        IBehavior instance;
        IBehaviorInvoker invoker;

        public BehaviorInstance(Type behaviorType, IBehavior instance)
        {
            this.instance = instance;
            Type = behaviorType;
            invoker = CreateInvoker(Type);
        }

        static IBehaviorInvoker CreateInvoker(Type type)
        {
            var behaviorInterface = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            var invokerType = typeof(BehaviorInvoker<,>).MakeGenericType(behaviorInterface.GetGenericArguments());
            return (IBehaviorInvoker) Activator.CreateInstance(invokerType);
        }

        public Type Type { get; private set; }

        public void Invoke(BehaviorContext context, Action<BehaviorContext> next)
        {
            invoker.Invoke(instance, context, next);
        }

        public void Initialize(PipelineInfo pipelineInfo)
        {
            instance.Initialize(pipelineInfo);
        }

        public Task Cooldown()
        {
            return instance.Cooldown();
        }

        public Task Warmup()
        {
            return instance.Warmup();
        }
    }
}