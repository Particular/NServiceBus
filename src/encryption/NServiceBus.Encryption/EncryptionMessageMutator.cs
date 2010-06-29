using System;
using System.Linq;
using Common.Logging;
using NServiceBus.MessageMutator;

namespace NServiceBus.Encryption
{
    /// <summary>
    /// Invokes the encryption service to encrypt/decrypt messages
    /// </summary>
    public class EncryptionMessageMutator:IMessageMutator
    {
        public IEncryptionService EncryptionService { get; set; }
 
        public IMessage MutateOutgoing(IMessage message)
        {
            var encryptedProperties = message.GetType().GetProperties()
             .Where(t => typeof(WireEncryptedString).IsAssignableFrom(t.PropertyType));

            foreach (var encryptedProperty in encryptedProperties)
            {
                if (EncryptionService == null)
                    throw new InvalidOperationException(String.Format("Cannot encrypt field {0} because no encryption service was configured.", encryptedProperty.Name));
              
                var encryptedString = (WireEncryptedString)encryptedProperty.GetValue(message, null);

                encryptedString.EncryptedValue = EncryptionService.Encrypt(encryptedString.Value);
                encryptedString.Value = null;

                Log.Debug(encryptedProperty.Name + " encrypted successfully");
    
            }
            return message;
        }

        public IMessage MutateIncoming(IMessage message)
        {
            var encryptedProperties = message.GetType().GetProperties()
             .Where(t => typeof(WireEncryptedString).IsAssignableFrom(t.PropertyType));

            foreach (var encryptedProperty in encryptedProperties)
            {
                if (EncryptionService == null)
                    throw new InvalidOperationException(String.Format("Cannot decrypt field {0} because no encryption service was configured.", encryptedProperty.Name));
               
                var encryptedString = (WireEncryptedString)encryptedProperty.GetValue(message, null);

                encryptedString.Value = EncryptionService.Decrypt(encryptedString.EncryptedValue);

                Log.Debug(encryptedProperty.Name + " decrypted successfully");
            }
            return message;
        }

        private readonly static ILog Log = LogManager.GetLogger(typeof(EncryptionMessageMutator));
    }
}