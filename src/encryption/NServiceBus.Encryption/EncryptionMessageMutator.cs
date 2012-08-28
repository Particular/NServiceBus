using System;
using System.Linq;
using Common.Logging;
using NServiceBus.MessageMutator;

namespace NServiceBus.Encryption
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using Config;

    /// <summary>
    /// Invokes the encryption service to encrypt/decrypt messages
    /// </summary>
    public class EncryptionMessageMutator : IMessageMutator
    {
        public IEncryptionService EncryptionService { get; set; }

        public object MutateOutgoing(object message)
        {
            EncryptObject(message);
            return message;
        }

        void EncryptObject(object target)
        {
            var properties = GetAllProperties(target);

            foreach (var property in properties)
            {
                if (property.IsEncryptedProperty())
                {
                    EncryptProperty(target, property);
                    continue;
                }

                if (property.PropertyType.IsPrimitive || IsSystemType(property.PropertyType))
                    continue;

                //recurse
                EncryptObject(property.GetValue(target, null));
            }
        }

        void EncryptProperty(object target, PropertyInfo encryptedProperty)
        {
            var valueToEncrypt = encryptedProperty.GetValue(target, null);

            if (valueToEncrypt == null)
                return;

            if (EncryptionService == null)
                throw new InvalidOperationException(
                    String.Format("Cannot encrypt field {0} because no encryption service was configured.",
                                  encryptedProperty.Name));

            if (valueToEncrypt is WireEncryptedString)
            {
                var encryptedString = (WireEncryptedString) valueToEncrypt;
                EncryptWireEncryptedString(encryptedString);

                if (!ConfigureEncryption.EnsureCompatibilityWithNSB2)
                {
                    //we clear the properties to avoid having the extra data serialized
                    encryptedString.EncryptedBase64Value = null;
                    encryptedString.Base64Iv = null;
                }
            }
            else
            {
                encryptedProperty.SetValue(target, EncryptUserSpecifiedProperty(valueToEncrypt), null);
            }

            Log.Debug(encryptedProperty.Name + " encrypted successfully");
        }


        public object MutateIncoming(object message)
        {
            DecryptObject(message);
            return message;
        }

        void DecryptObject(object target)
        {
            var properties = GetAllProperties(target);

            foreach (var property in properties)
            {
                if (property.IsEncryptedProperty())
                {
                    DecryptProperty(target, property);
                    continue;
                }

                if (property.PropertyType.IsPrimitive || IsSystemType(property.PropertyType))
                    continue;

                //recurse
                DecryptObject(property.GetValue(target, null));
            }
        }

        bool IsSystemType(Type propertyType)
        {
            var nameOfContainingAssembly = propertyType.Assembly.FullName.ToLower();

            return nameOfContainingAssembly.StartsWith("mscorlib") || nameOfContainingAssembly.StartsWith("system.core");
        }

        void DecryptProperty(object target, PropertyInfo property)
        {
          
            var encryptedValue = property.GetValue(target, null);

            if (encryptedValue == null)
                return;

            if (EncryptionService == null)
                throw new InvalidOperationException(
                    String.Format("Cannot decrypt field {0} because no encryption service was configured.", property.Name));

            if (encryptedValue is WireEncryptedString)
                Decrypt((WireEncryptedString) encryptedValue);
            else
            {
                property.SetValue(target, DecryptUserSpecifiedProperty(encryptedValue), null);
            }

            Log.Debug(property.Name + " decrypted successfully");
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
        static IEnumerable<PropertyInfo> GetAllProperties(object target)
        {
            if (target == null)
                return new List<PropertyInfo>();

            var messageType = target.GetType();

            if (!cache.ContainsKey(messageType))
                cache[messageType] = messageType.GetProperties()
                 .ToList();

            return cache[messageType];
        }
        readonly static IDictionary<Type, IEnumerable<PropertyInfo>> cache = new ConcurrentDictionary<Type, IEnumerable<PropertyInfo>>();

        readonly static ILog Log = LogManager.GetLogger(typeof(IEncryptionService));
    }
}