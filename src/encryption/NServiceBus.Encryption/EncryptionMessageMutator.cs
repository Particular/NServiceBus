using System;
using System.Linq;
using NServiceBus.Logging;
using NServiceBus.MessageMutator;

namespace NServiceBus.Encryption
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Invokes the encryption service to encrypt/decrypt messages
    /// </summary>
    public class EncryptionMessageMutator:IMessageMutator
    {
        public IEncryptionService EncryptionService { get; set; }

        public object MutateOutgoing(object message)
        {
            var encryptedProperties = GetEncryptedProperties(message);

            foreach (var encryptedProperty in encryptedProperties)
            {
                if (EncryptionService == null)
                    throw new InvalidOperationException(String.Format("Cannot encrypt field {0} because no encryption service was configured.", encryptedProperty.Name));
              
                var valueToEncrypt = encryptedProperty.GetValue(message, null);


                if (valueToEncrypt == null)
                    continue;

                if (valueToEncrypt is WireEncryptedString)
                    EncryptWireEncryptedString((WireEncryptedString) valueToEncrypt);
                else
                {
                    encryptedProperty.SetValue(message, EncryptUserSpecifiedProperty(valueToEncrypt),null);
                }
                    
            
                Log.Debug(encryptedProperty.Name + " encrypted successfully");
    
            }
            return message;
        }


        public object MutateIncoming(object message)
        {
            var encryptedProperties = GetEncryptedProperties(message);

            foreach (var encryptedProperty in encryptedProperties)
            {
                if (EncryptionService == null)
                    throw new InvalidOperationException(String.Format("Cannot decrypt field {0} because no encryption service was configured.", encryptedProperty.Name));
               
                var encryptedValue = encryptedProperty.GetValue(message, null);

                if(encryptedValue == null)
                    continue;

                if (encryptedValue is WireEncryptedString)
                    Decrypt((WireEncryptedString) encryptedValue);
                else
                {
                    encryptedProperty.SetValue(message, DecryptUserSpecifiedProperty(encryptedValue), null);             
                }

                Log.Debug(encryptedProperty.Name + " decrypted successfully");
            }
            return message;
        }

        string DecryptUserSpecifiedProperty(object encryptedValue)
        {
            var stringToDecrypt = encryptedValue as string;

            if (stringToDecrypt == null)
                throw new InvalidOperationException("Only string properties is supported for convention based encryption, please check your convention");

            var parts = stringToDecrypt.Split(new[] { '@' }, StringSplitOptions.None);

            return EncryptionService.Decrypt(new EncryptedValue
            {
                EncryptedBase64Value = parts[0],
                Base64Iv = parts[1]
            }); 
        }

        void Decrypt(WireEncryptedString encryptedValue)
        {
            encryptedValue.Value = EncryptionService.Decrypt(encryptedValue.EncryptedValue);
        }

        string EncryptUserSpecifiedProperty(object valueToEncrypt)
        {
            var stringToEncrypt = valueToEncrypt as string;

            if (stringToEncrypt == null)
                throw new InvalidOperationException("Only string properties is supported for convention based encryption, please check your convention");

            var encryptedValue = EncryptionService.Encrypt(stringToEncrypt);

            return string.Format("{0}@{1}", encryptedValue.EncryptedBase64Value, encryptedValue.Base64Iv);
        }

        void EncryptWireEncryptedString(WireEncryptedString wireEncryptedString)
        {
            wireEncryptedString.EncryptedValue = EncryptionService.Encrypt(wireEncryptedString.Value);
            wireEncryptedString.Value = null;

        }
        
        static IEnumerable<PropertyInfo> GetEncryptedProperties(object message)
        {
            var messageType = message.GetType();

            if (!cache.ContainsKey(messageType))
               cache[messageType] = messageType.GetProperties()
                .Where(property => property.IsEncryptedProperty())
                .ToList();

            return cache[messageType];
        }

        readonly static IDictionary<Type,IEnumerable<PropertyInfo>> cache = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>(); 
      
        readonly static ILog Log = LogManager.GetLogger(typeof(IEncryptionService));
    }
}