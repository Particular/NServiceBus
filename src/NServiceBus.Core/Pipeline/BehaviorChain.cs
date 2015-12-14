namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Janitor;
    using NServiceBus.Pipeline;

    class BehaviorChain : IDisposable
    {
        public BehaviorChain(IEnumerable<BehaviorInstance> behaviorList)
        {
            itemDescriptors = behaviorList.ToArray();
        }

        public Task Invoke(IBehaviorContext context)
        {
            Guard.AgainstNull(nameof(context), context);

            return InvokeNext(context, 0);
        }

        public void Dispose()
        {

        }

        Task InvokeNext(IBehaviorContext context, int currentIndex)
        {
            if (currentIndex == itemDescriptors.Length)
            {
                return TaskEx.Completed;
            }

            var behavior = itemDescriptors[currentIndex];

            return behavior.Invoke(context, newContext => InvokeNext(newContext, currentIndex + 1));
        }

        [SkipWeaving]
        BehaviorInstance[] itemDescriptors;
    }
}