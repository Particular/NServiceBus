using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NServiceBus.ObjectBuilder.CastleWindsor
{
    class InstanceReleasingMessageModule : IMessageModule
    {
        void IMessageModule.HandleBeginMessage()
        {
            
        }

        void IMessageModule.HandleEndMessage()
        {
            Builder.ReleaseResolvedInstances();
        }

        void IMessageModule.HandleError()
        {
            Builder.ReleaseResolvedInstances();
        }

        public static WindsorObjectBuilder Builder { get; set; }
    }
}
