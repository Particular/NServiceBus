namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using UnitOfWork;

    class UnitOfWorkBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
        public UnitOfWorkBehavior(bool hasUnitsOfWork)
        {
            this.hasUnitsOfWork = hasUnitsOfWork;
        }

        public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            if (hasUnitsOfWork)
            {
                return InvokeUnitsOfWork(context, next);
            }

            return next(context);
        }

        async Task InvokeUnitsOfWork(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
        {
            var unitsOfWork = new Stack<IManageUnitsOfWork>();

            try
            {
                foreach (var uow in context.Builder.BuildAll<IManageUnitsOfWork>())
                {
                    unitsOfWork.Push(uow);
                    await uow.Begin()
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }

                await next(context).ConfigureAwait(false);

                while (unitsOfWork.Count > 0)
                {
                    var popped = unitsOfWork.Pop();
                    await popped.End()
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }
            }
            catch (MessageDeserializationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var trailingExceptions = await AppendEndExceptions(unitsOfWork, exception).ConfigureAwait(false);
                if (trailingExceptions.Any())
                {
                    trailingExceptions.Insert(0, exception);
                    throw new AggregateException(trailingExceptions);
                }
                throw;
            }
        }

        static async Task<List<Exception>> AppendEndExceptions(Stack<IManageUnitsOfWork> unitsOfWork, Exception initialException)
        {
            var exceptionsToThrow = new List<Exception>();
            while (unitsOfWork.Count > 0)
            {
                var uow = unitsOfWork.Pop();
                try
                {
                    await uow.End(initialException)
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }
                catch (Exception endException)
                {
                    exceptionsToThrow.Add(endException);
                }
            }
            return exceptionsToThrow;
        }

        bool hasUnitsOfWork;
    }
}