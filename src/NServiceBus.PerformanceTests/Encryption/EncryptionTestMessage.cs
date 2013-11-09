using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace Runner.Encryption
{
    public class EncryptionTestMessage : MessageBase
    {
        public WireEncryptedString Secret { get; set; }
        public WireEncryptedString SecretField;
        public CreditCardDetails CreditCard { get; set; }

        public WireEncryptedString SecretThatIsNull { get; set; }

        public DateTime DateTime { get; set; }

        public List<CreditCardDetails> ListOfCreditCards { get; set; }

        public ArrayList ListOfSecrets { get; set; }

        public byte[] LargeByteArray { get; set; }
    }

    public class CreditCardDetails
    {
        public WireEncryptedString CreditCardNumber { get; set; }
    }
}
