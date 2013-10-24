using NServiceBus;
using System;
using System.Reflection;

namespace NServiceBus.Aop.Args
{
    public class HandlerAspectExceptionArgs : HandlerAspectArgs
    {
        public HandlerAspectExceptionArgs(object handler, IMessage message, MethodInfo methodinfo, Exception exception)
            : base(handler, message, methodinfo)
        {
            Exception = exception;
        }


        public Exception Exception
        {
            get; private set;
        }
    }
}
