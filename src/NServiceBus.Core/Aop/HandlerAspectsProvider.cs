using NServiceBus.Aop.Args;
using System;

namespace NServiceBus.Aop
{
    public class HandlerAspectsProvider
    {
        public virtual void OnEntry(HandlerAspectEntryArgs args)
        { }

        public virtual void OnExit(HandlerAspectArgs args)
        { }

        public virtual void OnSuccess(HandlerAspectArgs args)
        { }

        public virtual void OnException(HandlerAspectExceptionArgs args)
        { }
    }
}
