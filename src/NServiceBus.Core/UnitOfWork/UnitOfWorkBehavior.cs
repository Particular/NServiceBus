﻿namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Pipeline;
    using UnitOfWork;

    class UnitOfWorkBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
    {
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
                var hasUow = false;
                foreach (var uow in context.Builder.GetServices<IManageUnitsOfWork>())
                {
                    hasUow = true;
                    unitsOfWork.Push(uow);
                    await uow.Begin()
                        .ThrowIfNull()
                        .ConfigureAwait(false);
                }

                hasUnitsOfWork = hasUow;

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
            catch (Exception ex) when (!ex.IsCausedBy(context.CancellationToken))
            {
                var trailingExceptions = await AppendEndExceptions(unitsOfWork, ex, context.CancellationToken).ConfigureAwait(false);
                if (trailingExceptions.Any())
                {
                    trailingExceptions.Insert(0, ex);
                    throw new AggregateException(trailingExceptions);
                }
                throw;
            }
        }

        static async Task<List<Exception>> AppendEndExceptions(Stack<IManageUnitsOfWork> unitsOfWork, Exception initialException, CancellationToken cancellationToken)
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

        volatile bool hasUnitsOfWork = true;
    }
}