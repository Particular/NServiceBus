namespace NServiceBus.Pipeline
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using NServiceBus.Unicast.Transport;

    [DebuggerDisplay("{type.Name}")]
    class BehaviorInstance : IDisposable
    {
        readonly IBehavior instance;
        readonly Type type;
        readonly IBehaviorInvoker invoker;

        public BehaviorInstance(Type behaviorType, IBehavior instance)
        {
            this.instance = instance;
            type = behaviorType;
            invoker = CreateInvoker(type);
        }

        static IBehaviorInvoker CreateInvoker(Type type)
        {
            var behaviorInterface = type.GetInterfaces().First(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IBehavior<,>));
            var invokerType = typeof(BehaviorInvoker<,>).MakeGenericType(behaviorInterface.GetGenericArguments());
            return (IBehaviorInvoker) Activator.CreateInstance(invokerType);
        }

        public Type Type { get { return type; } }

        public void Invoke(BehaviorContext context, Action<BehaviorContext> next)
        {
            invoker.Invoke(instance, context, next);
        }

        public void Initialize(PipelineInfo pipelineInfo)
        {
            instance.Initialize(pipelineInfo);
        }

        public void OnStopped()
        {
            instance.OnStopped();
        }

        public void OnStarting()
        {
            instance.OnStarting();
        }

        void DisposeManaged()
        {
            var disposableBehavior = instance as IDisposable;
            if (disposableBehavior != null)
            {
                disposableBehavior.Dispose();
            }
        }

        public void Dispose()
        {
            //Injected via Fody
        }
    }
}