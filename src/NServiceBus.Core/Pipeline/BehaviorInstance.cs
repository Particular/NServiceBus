namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;

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

        public Type Type { get; }

        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return invoker.Invoke(instance, context, next);
        }
    }
}