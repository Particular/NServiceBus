namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.UnitOfWork;


    class UnitOfWorkBehavior : PhysicalMessageProcessingStageBehavior
    {
        public override void Invoke(Context context, Action next)
        {
            try
            {
                foreach (var uow in context.Builder.BuildAll<IManageUnitsOfWork>())
                {
                    unitsOfWork.Push(uow);
                    uow.Begin();
                }

                next();

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