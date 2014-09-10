namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    [Serializable]
    class AggregateExceptionEx : AggregateException
    {
        public AggregateExceptionEx(IEnumerable<Exception> exceptionsToThrow)
            : base(exceptionsToThrow)
        {
        }
        protected AggregateExceptionEx(SerializationInfo info, StreamingContext context):base(info, context)
        {
            
        }

        public AggregateExceptionEx(Exception exception):base(exception)
        {
        }

        public static Exception UnwindIfSingleException(Exception exception)
        {
            var aggregateExceptionEx = exception as AggregateExceptionEx;
            if (aggregateExceptionEx != null)
            {
                var exceptions = aggregateExceptionEx.InnerExceptions;
                if (exceptions.Count == 1)
                {
                    return exceptions.First();
                }
            }
            return exception;
        }
    }
}