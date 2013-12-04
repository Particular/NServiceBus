namespace NServiceBus.Core.Tests.DataBus
{
    using NServiceBus.DataBus;
    using NUnit.Framework;
    using Rhino.Mocks;

    class on_the_bus
    {
        protected IDataBus dataBus;
        protected DataBusSendBehavior sendBehavior;
        protected DataBusReceiveBehavior receiveBehavior;
    
        [SetUp]
        public void SetUp()
        {
            dataBus = MockRepository.GenerateMock<IDataBus>();
            
            receiveBehavior = new DataBusReceiveBehavior
            {
                DataBus = dataBus,
                DataBusSerializer = new DefaultDataBusSerializer()
            };

            sendBehavior = new DataBusSendBehavior
            {
                DataBus = dataBus,
                DataBusSerializer = new DefaultDataBusSerializer()
            };
        }

    }
}