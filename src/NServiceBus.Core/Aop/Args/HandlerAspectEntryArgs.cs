using NServiceBus;
using System.Reflection;

namespace NServiceBus.Aop.Args
{
    public class HandlerAspectEntryArgs : HandlerAspectArgs
    {
        public HandlerAspectEntryArgs(object handler, IMessage message, MethodInfo methodinfo)
            : base(handler, message, methodinfo)
        {
            ContinueExecution = true;
        }


        public bool ContinueExecution
        {
            get; set;
        }
    }
}
