namespace NServiceBus.UnitOfWork
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.Serialization;
    using Pipeline;
    using Pipeline.Contexts;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class UnitOfWorkBehavior : IBehavior<ReceivePhysicalMessageContext>
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
            catch (SerializationException)
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