namespace NServiceBus.Encryption
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using MessageMutator;
    using Utils.Reflection;

    /// <summary>
    /// Invokes the encryption service to encrypt/decrypt messages
    /// </summary>
    public class EncryptionMessageMutator : IMessageMutator
    {
        public IEncryptionService EncryptionService { get; set; }

        public object MutateOutgoing(object message)
        {
            ForEachMember(message, EncryptMember, IsEncryptedMember);

            return message;
        }

        public object MutateIncoming(object message)
        {
            ForEachMember(message, DecryptMember, IsEncryptedMember);

            return message;
        }

        static bool IsIndexedProperty(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;

            if (propertyInfo != null)
            {
                return propertyInfo.GetIndexParameters().Length > 0;
            }

            return false;
        }

        static bool IsEncryptedMember(MemberInfo arg)
        {
            
            var propertyInfo = arg as PropertyInfo;
            if (propertyInfo != null)
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    if (MessageConventionExtensions.IsEncryptedProperty(propertyInfo))
                    {
                        throw new Exception("Cannot encrypt or decrypt indexed properties that return a WireEncryptedString.");
                    }

                    return false;
                }

                return MessageConventionExtensions.IsEncryptedProperty(propertyInfo);
            }

            var fieldInfo = arg as FieldInfo;
            if (fieldInfo != null)
            {
                return fieldInfo.FieldType == typeof(WireEncryptedString);
            }

            return false;
        }

        void ForEachMember(object root, Action<object, MemberInfo> action, Func<MemberInfo, bool> appliesTo)
        {
            if (root == null || visitedMembers.Contains(root))
            {
                return;
            }

            visitedMembers.Add(root);

            var members = GetFieldsAndProperties(root);

            foreach (var member in members)
            {
                if (appliesTo(member))
                {
                    action(root, member);
                }

                //don't recurse over primitives or system types
                if (member.ReflectedType.IsPrimitive || member.ReflectedType.IsSystemType())
                {
                    continue;
                }

                if (IsIndexedProperty(member))
                {
                    continue;
                }
                
                var child = member.GetValue(root);

                var items = child as IEnumerable;
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        //don't recurse over primitives or system types
                        if (item.GetType().IsPrimitive || item.GetType().IsSystemType())
                        {
                            break;
                        }

                        ForEachMember(item, action, appliesTo);
                    }
                }
                else
                {
                    ForEachMember(child, action, appliesTo);
                }
            }
        }

        void EncryptMember(object target, MemberInfo member)
        {
            var valueToEncrypt = member.GetValue(target);

            if (valueToEncrypt == null)
            {
                return;
            }

            if (EncryptionService == null)
            {
                throw new Exception(String.Format("Cannot encrypt field {0} because no encryption service was configured.",member.Name));
            }

            var wireEncryptedString = valueToEncrypt as WireEncryptedString;
            if (wireEncryptedString != null)
            {
                var encryptedString = wireEncryptedString;
                EncryptWireEncryptedString(encryptedString);

                //we clear the properties to avoid having the extra data serialized
                encryptedString.EncryptedBase64Value = null;
                encryptedString.Base64Iv = null;
            }
            else
            {
                member.SetValue(target, EncryptUserSpecifiedProperty(valueToEncrypt));
            }

            Log.Debug(member.Name + " encrypted successfully");
        }

        void DecryptMember(object target, MemberInfo property)
        {
            var encryptedValue = property.GetValue(target);

            if (encryptedValue == null)
            {
                return;
            }

            if (EncryptionService == null)
            {
                throw new Exception(String.Format("Cannot decrypt field {0} because no encryption service was configured.", property.Name));
            }

            var wireEncryptedString = encryptedValue as WireEncryptedString;
            if (wireEncryptedString != null)
            {
                Decrypt(wireEncryptedString);
            }
            else
            {
                property.SetValue(target, DecryptUserSpecifiedProperty(encryptedValue));
            }

            Log.Debug(property.Name + " decrypted successfully");
        }

        string DecryptUserSpecifiedProperty(object encryptedValue)
        {
            var stringToDecrypt = encryptedValue as string;

            if (stringToDecrypt == null)
            {
                throw new Exception("Only string properties is supported for convention based encryption, please check your convention");
            }

            var parts = stringToDecrypt.Split(new[] { '@' }, StringSplitOptions.None);

            return EncryptionService.Decrypt(new EncryptedValue
            {
                EncryptedBase64Value = parts[0],
                Base64Iv = parts[1]
            });
        }

        void Decrypt(WireEncryptedString encryptedValue)
        {
            if (encryptedValue.EncryptedValue == null)
            {
                throw new Exception("Encrypted property is missing encryption data");
            }

            encryptedValue.Value = EncryptionService.Decrypt(encryptedValue.EncryptedValue);
        }

        string EncryptUserSpecifiedProperty(object valueToEncrypt)
        {
            var stringToEncrypt = valueToEncrypt as string;

            if (stringToEncrypt == null)
            {
                throw new Exception("Only string properties is supported for convention based encryption, please check your convention");
            }

            var encryptedValue = EncryptionService.Encrypt(stringToEncrypt);

            return string.Format("{0}@{1}", encryptedValue.EncryptedBase64Value, encryptedValue.Base64Iv);
        }

        void EncryptWireEncryptedString(WireEncryptedString wireEncryptedString)
        {
            wireEncryptedString.EncryptedValue = EncryptionService.Encrypt(wireEncryptedString.Value);
            wireEncryptedString.Value = null;

        }
        static IEnumerable<MemberInfo> GetFieldsAndProperties(object target)
        {
            if (target == null)
            {
                return new List<MemberInfo>();
            }

            var messageType = target.GetType();

            IEnumerable<MemberInfo> members;
            if (!cache.TryGetValue(messageType, out members))
            {
                cache[messageType] = messageType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m is FieldInfo || m is PropertyInfo)
                    .ToList();
            }

            return members;
        }

        HashSet<object> visitedMembers = new HashSet<object>();

        static ConcurrentDictionary<Type, IEnumerable<MemberInfo>> cache = new ConcurrentDictionary<Type, IEnumerable<MemberInfo>>();

        static ILog Log = LogManager.GetLogger(typeof(IEncryptionService));
    }
}
