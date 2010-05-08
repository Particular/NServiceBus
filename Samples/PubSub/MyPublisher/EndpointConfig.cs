using NServiceBus;
using NServiceBus.Grid.MessageHandlers;
using NServiceBus.Sagas.Impl;

namespace MyPublisher
{
    class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher {}
}
