namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ObjectBuilder;
    using UnitOfWork;

    class UnitOfWorkRunner
    {
        public IBuilder Builder;
        Stack<IManageUnitsOfWork> unitsOfWork = new Stack<IManageUnitsOfWork>();

        public void Begin()
        {
            foreach (var uow in Builder.BuildAll<IManageUnitsOfWork>())
            {
                unitsOfWork.Push(uow);
                uow.Begin();
            }
        }

        public void End()
        {
            while (unitsOfWork.Count > 0)
            {
                var uow = unitsOfWork.Pop();
                uow.End();
            }
        }

        public void AppendEndExceptionsAndRethrow(Exception parentException)
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
    }
}