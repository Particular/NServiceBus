using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NServiceBus.Faults;
using NServiceBus.Unicast.Queuing;
using NUnit.Framework;
using Rhino.Mocks;

namespace NServiceBus.Unicast.Transport.Transactional.Tests
{
    [TestFixture]
    public class When_a_message_processing_fails
    {
        [Test]
        public void The_original_exception_should_be_passed_to_the_fault_manager()
        {
            //var failureManager = MockRepository.GenerateStub<IManageMessageFailures>();
            //var messageSender = MockRepository.GenerateStub<IReceiveMessages>();

            //messageSender.Stub(x => x.HasMessage()).Return(true).Repeat.Twice();

            //messageSender.Stub(x => x.Receive(true)).Return(new TransportMessage
            //                                                    {
            //                                                        Id = "123"
            //                                                    }).Repeat.Twice();

            //var transport = new TransactionalTransport
            //                    {
            //                        MessageQueue = messageSender,
            //                        FailureManager = failureManager,
            //                        IsTransactional = true,
            //                        NumberOfWorkerThreads = 1,
            //                        MaxRetries = 1
            //                    };

            //transport.TransportMessageReceived += TransportMessageReceived;

            //((ITransport)transport).Start("");

            //Thread.Sleep(500);

            //failureManager.AssertWasCalled(x => x.ProcessingAlwaysFailsForMessage(Arg<TransportMessage>.Is.Anything, Arg<ApplicationException>.Is.TypeOf));
        }

        static void TransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            throw new TransportMessageHandlingFailedException(new ApplicationException( "A user exception"));
        }

    }
}
