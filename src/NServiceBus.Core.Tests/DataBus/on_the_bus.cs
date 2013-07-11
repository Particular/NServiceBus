namespace NServiceBus.Core.Tests.DataBus
{
    using NServiceBus.DataBus;
    using NServiceBus.MessageHeaders;
    using NServiceBus.MessageMutator;
    using NUnit.Framework;
    using Rhino.Mocks;

    public class on_the_bus
    {
        protected IDataBus dataBus;
        protected IMutateOutgoingMessages outgoingMutator;
        protected IMutateIncomingMessages incomingMutator;


        readonly MessageHeaderManager headerManager = new MessageHeaderManager();
            
        [SetUp]
        public void SetUp()
        {
            ExtensionMethods.SetHeaderAction = headerManager.SetHeader;
            ExtensionMethods.GetHeaderAction = headerManager.GetHeader;

            dataBus = MockRepository.GenerateMock<IDataBus>();
            
            var databusMutator = new DataBusMessageMutator(dataBus, new DefaultDataBusSerializer());
            
            incomingMutator = databusMutator;
            outgoingMutator = databusMutator;
        }

    }
}