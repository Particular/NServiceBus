using System.Reflection;

namespace NServiceBus.Aop.Args
{
    public class HandlerAspectArgs
    {
        public HandlerAspectArgs(object handler, IMessage message, MethodInfo methodInfo)
        {
            Handler = handler;

            Message = message;

            MethodInfo = methodInfo;
        }


        public object Handler
        {
            get; private set;
        }

        public IMessage Message
        {
            get; private set;
        }

        public MethodInfo MethodInfo
        {
            get; private set;
        }
    }
}
