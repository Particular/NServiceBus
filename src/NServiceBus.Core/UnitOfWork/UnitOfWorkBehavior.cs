namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Pipeline;
    using UnitOfWork;

    class UnitOfWorkBehavior : Behavior<PhysicalMessageProcessingContext>
    {
        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            var unitsOfWork = new Stack<IManageUnitsOfWork>();

            try
            {
                foreach (var uow in context.Builder.BuildAll<IManageUnitsOfWork>())
                {
                    unitsOfWork.Push(uow);
                    await uow.Begin()
                        .ConfigureAwait(false);
                }

                await next().ConfigureAwait(false);

                while (unitsOfWork.Count > 0)
                {
                    await unitsOfWork.Pop()
                        .End().ConfigureAwait(false);
                }
            }
            catch (MessageDeserializationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var trailingExceptions = AppendEndExceptions(unitsOfWork, exception);
                if (trailingExceptions.Any())
                {
                    trailingExceptions.Insert(0, exception);
                    throw new AggregateException(trailingExceptions);
                }
                throw;
            }
        }

        List<Exception> AppendEndExceptions(Stack<IManageUnitsOfWork> unitsOfWork, Exception initialException)
        {
            var exceptionsToThrow = new List<Exception>();
            while (unitsOfWork.Count > 0)
            {
                var uow = unitsOfWork.Pop();
                try
                {
                    uow.End(initialException);
                }
                catch (Exception endException)
                {
                    exceptionsToThrow.Add(endException);
                }
            }
            return exceptionsToThrow;
        }
    }
}