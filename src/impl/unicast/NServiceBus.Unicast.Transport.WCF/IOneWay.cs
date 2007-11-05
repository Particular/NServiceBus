using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace NServiceBus.Unicast.Transport.WCF
{
    [ServiceContract()]
    public interface IOneWay
    {
        [OperationContract(IsOneWay = true, Action="*")]
        void Process(Message m);
    }
}
