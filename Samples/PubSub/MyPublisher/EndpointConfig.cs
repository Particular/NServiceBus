using MyMessages;
using NServiceBus;

namespace MyPublisher
{
    class EndpointConfig :  IConfigureThisEndpoint, AsA_Publisher,
                            IAmResponsibleForMessages<IMyEvent>, 
                            IAmResponsibleForMessages<EventMessage>{}
}
