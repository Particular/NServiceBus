using System;
using MyMessages;
using MyMessages.DataBus;
using MyMessages.Other;
using NServiceBus;

namespace MyPublisher
{
    using System.Threading;
    using System.Transactions;

    using MyMessages.Publisher;
    using MyMessages.Subscriber1;
    using MyMessages.Subscriber2;
    using MyMessages.SubscriberNMS;

    using MyPublisher.Scheduling;

    public class MyMessage1Handler : IHandleMessages<MyRequest1>
    {
        public IBus Bus { get; set; }

        public void Handle(MyRequest1 message)
        {
            Console.WriteLine("Message1");
            using (var tx = new TransactionScope(TransactionScopeOption.RequiresNew))
            {
                var localCommand = new LocalCommand { CommandId = Guid.NewGuid(), };
                this.Bus.SendLocal(localCommand);

                tx.Complete();
            }

            throw new Exception();
        }
    }

    internal class TestSinglePhaseCommit : ISinglePhaseNotification
    {
        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            Console.WriteLine("Tx Prepare");
            preparingEnlistment.Prepared();
        }
        public void Commit(Enlistment enlistment)
        {
            Console.WriteLine("Tx Commit");
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            Console.WriteLine("Tx Rollback");
            enlistment.Done();
        }
        public void InDoubt(Enlistment enlistment)
        {
            Console.WriteLine("Tx InDoubt");
            enlistment.Done();
        }
        public void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment)
        {
            Console.WriteLine("Tx SinglePhaseCommit");
            singlePhaseEnlistment.Committed();
            //singlePhaseEnlistment.Aborted();
        }
    }

    public class ServerEndpoint : IWantToRunWhenBusStartsAndStops
    {
        private static Random randomizer = new Random();

        private int nextEventToPublish = 0;
        private int nextCommandToPublish = 0;
        private bool failSagaCompletion = false;

        public IBus Bus { get; set; }

        public void Start()
        {
            Console.WriteLine("Press 'e' to publish an IEvent, EventMessage, and AnotherEventMessage alternately.");
            Console.WriteLine("Press 'b' to send a command with large payload to Subscriber1");
            Console.WriteLine("Press 'c' to send a command to Subscriber1, Subscriber2, SubscriberNMS alternately");
            Console.WriteLine("Press 's' to start a saga locally");
            Console.WriteLine("Press 'x' to start a saga locally and complete it before timeout. Completion fails every second time");
            Console.WriteLine("Press 'd' to defer a command locally");
            Console.WriteLine("Press 'l' to send a command locally");
            Console.WriteLine("Press 'n' to send a notification.");
            Console.WriteLine("Press 't' to schedule a task.");
            Console.WriteLine("Press 'q' to exit");

            while (true)
            {
                var key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case 'q':
                        return;
                    case 'e':
                        this.PublishEvent();
                        break;
                    case 'b':
                        this.SendOverDataBus();
                        break;
                    case 'c':
                        this.SendCommand();
                        break;
                    case 's':
                        this.StartSaga();
                        break;
                    case 'x':
                        this.StartSagaAndCompleteBeforeTimeout(this.failSagaCompletion);
                        this.failSagaCompletion = !this.failSagaCompletion;
                        break;
                    case 'd':
                        this.DeferCommand();
                        break;
                    case 'l':
                        this.SendCommandLocal();
                        break;
                    case 'n':
                        this.SendNotification();
                        break;
                    case 't':
                        this.ScheduleTask();
                        break;
                    case 'z':
                        this.Test();
                        break;
                }
            }
        }
        
        private static TestSinglePhaseCommit x = new TestSinglePhaseCommit();
        private static Guid guid = Guid.NewGuid();
        private void Test()
        {
            {
                Console.WriteLine("Send 1");

                using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromSeconds(10)))
                {
                    Transaction.Current.EnlistDurable(guid, x, EnlistmentOptions.None);

                    var commandMessage = this.Bus.CreateInstance<MyRequest1>();
                    commandMessage.CommandId = Guid.NewGuid();
                    commandMessage.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
                    commandMessage.Duration = TimeSpan.FromSeconds(99999D);

                    this.Bus.Send(commandMessage);
                    scope.Complete();
                }

                Console.WriteLine("Done");
            }
        }/*

        private void Test()
        {
            Bus.SendLocal<MyRequest1>(m => { });
        }*/

        private void SendOverDataBus()
        {
            Bus.Send<MessageWithLargePayload>(m =>
            {
                m.SomeProperty =
                    "This message contains a large blob that will be sent on the data bus";
                m.LargeBlob =
                    new DataBusProperty<byte[]>(new byte[1024 * 1024 * 5]);
                //5MB
            });
        }

        private void ScheduleTask()
        {
            Bus.SendLocal(new ScheduleATask());
        }

        private void SendNotification()
        {
            this.Bus.SendEmail(new MailMessage("test@nservicebus.com", "udidahan@nservicebus.com"));
        }

        private void StartSaga()
        {
            var startSagaMessage = new StartSagaMessage { OrderId = Guid.NewGuid() };

            this.Bus.SendLocal(startSagaMessage);

            Console.WriteLine("Starting saga with for order id {0}.", startSagaMessage.OrderId);
            Console.WriteLine("==========================================================================");
        }

        private void StartSagaAndCompleteBeforeTimeout(bool fail)
        {
            var startSagaMessage = new StartSagaMessage { OrderId = Guid.NewGuid() };
            this.Bus.SendLocal(startSagaMessage);

            Console.WriteLine("Starting saga with for order id {0}.", startSagaMessage.OrderId);
            Console.WriteLine("==========================================================================");

            Thread.Sleep(1000);

            var completeSagaMessage = new CompleteSagaMessage
                {
                    OrderId = startSagaMessage.OrderId,
                    ThrowDuringCompletion = fail
                };

            this.Bus.SendLocal(completeSagaMessage);

        }

        private void SendCommandLocal()
        {
            var localCommand = new LocalCommand { CommandId = Guid.NewGuid(), };

            this.Bus.SendLocal(localCommand);

            Console.WriteLine("Sent command with Id {0}.", localCommand.CommandId);
            Console.WriteLine("==========================================================================");
        }

        private void DeferCommand()
        {
            TimeSpan delay = TimeSpan.FromSeconds(randomizer.Next(2, 6));

            var deferredMessage = new DeferedMessage();

            this.Bus.Defer(delay, deferredMessage);

            Console.WriteLine("{0} - Sent a message with id {1} to be processed in {2}.", DateTime.Now.ToLongTimeString(), deferredMessage.Id, delay.ToString());
            Console.WriteLine("==========================================================================");
        }

        private void SendCommand()
        {
            IMyCommand commandMessage;

            switch (nextCommandToPublish)
            {
                case 0:
                    commandMessage = this.Bus.CreateInstance<MyRequest1>();
                    nextCommandToPublish = 1;
                    break;
                case 1:
                    commandMessage = this.Bus.CreateInstance<IMyRequest2>();
                    nextCommandToPublish = 2;
                    break;
                case 2:
                    commandMessage = new MyRequestNMS();
                    nextCommandToPublish = 3;
                    break;
                default:
                    commandMessage = new MyRequest1();
                    commandMessage.ThrowExceptionDuringProcessing = true;
                    nextCommandToPublish = 0;
                    break;
            }

            commandMessage.CommandId = Guid.NewGuid();
            commandMessage.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
            commandMessage.Duration = TimeSpan.FromSeconds(99999D);

            this.Bus.Send(commandMessage).Register<ResponseCode>(response =>
                {
                    Console.WriteLine("Received Response to request {0}: {1}", commandMessage.CommandId, response);
                    Console.WriteLine("==========================================================================");
                });

            Console.WriteLine("Sent command with Id {0}.", commandMessage.CommandId);
            Console.WriteLine("==========================================================================");
        }

        private void PublishEvent()
        {
            IMyEvent eventMessage;

            switch (nextEventToPublish)
            {
                case 0:
                    eventMessage = this.Bus.CreateInstance<IMyEvent>();
                    nextEventToPublish = 1;
                    break;
                case 1:
                    eventMessage = new EventMessage();
                    nextEventToPublish = 2;
                    break;
                default:
                    eventMessage = new AnotherEventMessage();
                    nextEventToPublish = 0;
                    break;
            }

            eventMessage.EventId = Guid.NewGuid();
            eventMessage.Time = DateTime.Now.Second > -1 ? (DateTime?)DateTime.Now : null;
            eventMessage.Duration = TimeSpan.FromSeconds(99999D);

            this.Bus.Publish(eventMessage);

            Console.WriteLine("Published event with Id {0}.", eventMessage.EventId);
            Console.WriteLine("==========================================================================");
        }

        public void Stop()
        {

        }
    }
}