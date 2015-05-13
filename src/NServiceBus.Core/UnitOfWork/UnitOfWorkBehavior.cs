namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.UnitOfWork;


    class UnitOfWorkBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override async Task Invoke(Context context, Func<Task> next)
        {
            try
            {
                foreach (var uow in context.Builder.BuildAll<IManageUnitsOfWork>())
                {
                    unitsOfWork.Push(uow);
                    uow.Begin();
                }

                await next();

                while (unitsOfWork.Count > 0)
                {
                    unitsOfWork.Pop().End();
                }
            }
            catch (MessageDeserializationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                var trailingExceptions = AppendEndExceptionsAndRethrow(exception);
                if (trailingExceptions.Any())
                {
                    trailingExceptions.Insert(0, exception);
                    throw new AggregateException(trailingExceptions);
                }
                throw;
            }
        }

        List<Exception> AppendEndExceptionsAndRethrow(Exception initialException)
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

        Stack<IManageUnitsOfWork> unitsOfWork = new Stack<IManageUnitsOfWork>();

    }
}