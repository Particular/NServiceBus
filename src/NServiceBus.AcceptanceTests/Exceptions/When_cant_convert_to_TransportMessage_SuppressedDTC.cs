﻿﻿namespace NServiceBus.AcceptanceTests.Exceptions
 {
     using System;
     using System.Linq;
     using NServiceBus.AcceptanceTesting;
     using NServiceBus.AcceptanceTests.EndpointTemplates;
     using NServiceBus.Timeout.Core;
     using NUnit.Framework;

     public class When_cant_convert_to_TransportMessage_SuppressedDTC : NServiceBusAcceptanceTest
     {
         [Test]
         public void Should_send_message_to_error_queue()
         {
             Scenario.Define<Context>()
                     .WithEndpoint<Sender>(b => b.Given(bus => bus.Send(new Message())))
                     .WithEndpoint<Receiver>()
                     .Done(c => c.GetAllLogs().Any(l => l.Level == "error"))
                     .Repeat(r => r.For(ScenarioDescriptors.Transports.Msmq))
                     .Should(c =>
                     {
                         var logs = c.GetAllLogs();
                         Assert.True(logs.Any(l => l.Message.Contains("is corrupt and will be moved to")));
                     })
                     .Run();
         }

         public class Context : ScenarioContext
         {
         }

         public class Sender : EndpointConfigurationBuilder
         {
             public Sender()
             {
                 Configure.Transactions.Advanced(settings => settings.DisableDistributedTransactions());
                 EndpointSetup<DefaultServer>(c => c.SuppressOutdatedTimeoutDispatchWarning())
                     .AddMapping<Message>(typeof(Receiver));
             }
         }

         public class Receiver : EndpointConfigurationBuilder
         {
             public Receiver()
             {
                 SerializerCorrupter.Corrupt();
                 Configure.Transactions.Advanced(settings => settings.DisableDistributedTransactions());
                 EndpointSetup<DefaultServer>(c => c.SuppressOutdatedTimeoutDispatchWarning())
                     .AllowExceptions();
             }
         }

         [Serializable]
         public class Message : IMessage
         {
         }
     }
 }