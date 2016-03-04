namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;

    [DebuggerDisplay("{Type.Name}")]
    class BehaviorInstance
    {
        public BehaviorInstance(Type behaviorType, IBehavior instance)
        {
            this.instance = instance;
            Type = behaviorType;
            invoker = CreateInvoker(Type);
        }

        public Type Type { get; }

        static IBehaviorInvoker CreateInvoker(Type type)
        {
            var behaviorInterface = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            var invokerType = typeof(BehaviorInvoker<,>).MakeGenericType(behaviorInterface.GetGenericArguments());
            return (IBehaviorInvoker) Activator.CreateInstance(invokerType);
        }

        public Task Invoke(IBehaviorContext context, Func<IBehaviorContext, Task> next)
        {
            return invoker.Invoke(instance, context, next);
        }

        IBehavior instance;
        IBehaviorInvoker invoker;
    }
}