using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NServiceBus.Core.Tests.Encryption
{
    [TestFixture]
    public class Issue_1630 : WireEncryptedStringContext
    {
        [Test]
        public void Reflection_should_not_cause_overhead()
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            for (int i = 0; i < 100000; i++)
            {
                // need a new instance of a message each time
                var message = new Customer
                {
                    Secret = MySecretMessage,
                    SecretField = MySecretMessage,
                    CreditCard = new CreditCardDetails { CreditCardNumber = MySecretMessage },
                    LargeByteArray = new byte[1], // the length of the array is not the issue now
                    ListOfCreditCards =
                        new List<CreditCardDetails>
                        {
                            new CreditCardDetails {CreditCardNumber = MySecretMessage},
                            new CreditCardDetails {CreditCardNumber = MySecretMessage}
                        }
                };
                message.ListOfSecrets = new ArrayList(message.ListOfCreditCards);

                stopWatch.Start();

                mutator.MutateOutgoing(message);

                stopWatch.Stop();
            }

            Assert.Less(stopWatch.ElapsedMilliseconds, 2000, "Should perform under 2000ms.");
        }
    }
}
