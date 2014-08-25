namespace NServiceBus.Testing
{
    using Transports;

    class FakeTestTransport : TransportDefinition
    {
        public override string GetSubScope(string address, string qualifier)
        {
            return address + "." + qualifier;
        }
    }
}