using NServiceBus.Aop.Args;
using System;

namespace NServiceBus.Aop
{
    public class IUseHandlerAspectsAttribute : Attribute
    {
        public IUseHandlerAspectsAttribute(Type aspectProvider)
        {
            AspectProvider = aspectProvider;
        }


        public Type AspectProvider
        {
            get; private set;
        }


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
