namespace NServiceBus.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using Pipeline;
    using Pipeline.Contexts;

    class UnitOfWorkBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
       public void Invoke(ReceivePhysicalMessageContext context, Action next)
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
            catch (Exception exception)
            {
                AppendEndExceptionsAndRethrow(exception);
            }
        }

        void AppendEndExceptionsAndRethrow(Exception parentException)
        {
            var exceptionsToThrow = new List<Exception>
                                    {
                                        parentException
                                    };
            while (unitsOfWork.Count > 0)
            {
                var uow = unitsOfWork.Pop();
                try
                {
                    uow.End(parentException);
                }
                catch (Exception exception)
                {
                    exceptionsToThrow.Add(exception);
                }
            }
            throw new AggregateException(exceptionsToThrow);
        }

        Stack<IManageUnitsOfWork> unitsOfWork = new Stack<IManageUnitsOfWork>();

    }
}