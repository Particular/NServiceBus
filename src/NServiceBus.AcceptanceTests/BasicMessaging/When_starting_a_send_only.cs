﻿﻿namespace NServiceBus.AcceptanceTests.BasicMessaging
  {
      using System;
      using NUnit.Framework;

      public class When_starting_a_send_only : NServiceBusAcceptanceTest
      {
          [Test]
          public void Should_not_need_audit_or_fault_forwarding_config_to_start()
          {
              using ((IDisposable)Configure.With(new Type[]
                                  {
                                  })
                  .DefaultBuilder()
                  .UnicastBus()
                  .SendOnly())
              {
              }

          }
      }
  }