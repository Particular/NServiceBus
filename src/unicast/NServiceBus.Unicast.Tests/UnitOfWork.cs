namespace NServiceBus.Unicast.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Transport;
    using UnitOfWork;

    [TestFixture]
    public class When_processing_a_message_successfully : using_the_unicastbus
    {
        [Test]
        public void Should_invoke_all_the_uows()
        {
            var beginCalled = false;
            var endCalled = false;

            var uow = new TestUnitOfWork
                          {
                              OnBegin = () => { beginCalled = true; },
                              OnEnd = (ex) => { if(ex != null) throw new Exception("Failure wasn't expected"); endCalled = true; }
                          };

            RegisterUow(uow);
            ReceiveMessage(Helpers.EmptyTransportMessage());

            Assert.True(beginCalled);
            Assert.True(endCalled);
        }


        void ReceiveMessage(TransportMessage transportMessage)
        {
            Transport.FakeTransportMessageReceived(transportMessage);
        }
    }

    public class TestUnitOfWork:IManageUnitsOfWork
    {
        public Action<Exception> OnEnd = (ex) => { };
        public Action OnBegin = () => { };

        public void Begin()
        {
            OnBegin();
        }

        public void End(Exception ex = null)
        {
            OnEnd(ex);
        }
    }

    public class FakeTransport : ITransport
   {
       public void Dispose()
       {
           
       }

       public void Start(string inputqueue)
       {
       }

       public void Start(Address localAddress)
       {
       }

       public void ChangeNumberOfWorkerThreads(int targetNumberOfWorkerThreads)
       {
       }

       public void AbortHandlingCurrentMessage()
       {
           
       }

       public int NumberOfWorkerThreads
       {
           get { return 1; }
       }

       public event EventHandler<TransportMessageReceivedEventArgs> TransportMessageReceived;
       public event EventHandler StartedMessageProcessing;
       public event EventHandler FinishedMessageProcessing;
       public event EventHandler<FailedMessageProcessingEventArgs> FailedMessageProcessing;

        public void FakeTransportMessageReceived(TransportMessage transportMessage)
        {
            TransportMessageReceived(this,new TransportMessageReceivedEventArgs(transportMessage));
        }
   }

    class Helpers
    {
        public static  TransportMessage EmptyTransportMessage()
        {
            return new TransportMessage
                       {
                           Headers = new Dictionary<string, string>()
                       };
        }
    }
}