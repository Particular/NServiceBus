namespace NServiceBus.Encryption
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Logging;
    using Utils.Reflection;

    class EncryptionMutator
    {
        IEncryptionService encryptionService;
        Conventions conventions;

        public EncryptionMutator(IEncryptionService encryptionService, Conventions conventions)
        {
            this.encryptionService = encryptionService;
            this.conventions = conventions;
        }

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

        bool IsEncryptedMember(MemberInfo arg)
        {
            var propertyInfo = arg as PropertyInfo;
            if (propertyInfo != null)
            {
                if (propertyInfo.GetIndexParameters().Length > 0)
                {
                    if (conventions.IsEncryptedProperty(propertyInfo))
                    {
                        throw new Exception("Cannot encrypt or decrypt indexed properties that return a WireEncryptedString.");
                    }

                    return false;
                }

                return conventions.IsEncryptedProperty(propertyInfo);
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

                // don't try to recurse over members of WireEncryptedString
                if (member.DeclaringType == typeof(WireEncryptedString))
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

            var wireEncryptedString = valueToEncrypt as WireEncryptedString;
            if (wireEncryptedString != null)
            {
                var encryptedString = wireEncryptedString;
                EncryptWireEncryptedString(encryptedString);
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

            return encryptionService.Decrypt(new EncryptedValue
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

            encryptedValue.Value = encryptionService.Decrypt(encryptedValue.EncryptedValue);
        }

        string EncryptUserSpecifiedProperty(object valueToEncrypt)
        {
            var stringToEncrypt = valueToEncrypt as string;

            if (stringToEncrypt == null)
            {
                throw new Exception("Only string properties is supported for convention based encryption, please check your convention");
            }

            var encryptedValue = encryptionService.Encrypt(stringToEncrypt);

            return string.Format("{0}@{1}", encryptedValue.EncryptedBase64Value, encryptedValue.Base64Iv);
        }

        void EncryptWireEncryptedString(WireEncryptedString wireEncryptedString)
        {
            wireEncryptedString.EncryptedValue = encryptionService.Encrypt(wireEncryptedString.Value);
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
            if (!cache.TryGetValue(messageType.TypeHandle, out members))
            {
                cache[messageType.TypeHandle] = members = messageType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m =>
                    {
                        var fieldInfo = m as FieldInfo;
                        if (fieldInfo != null)
                        {
                            return !fieldInfo.IsInitOnly;
                        }

                        var propInfo = m as PropertyInfo;
                        if (propInfo != null)
                        {
                            return propInfo.CanWrite;
                        }

                        return false;
                    })
                    .ToList();
            }

            return members;
        }

        HashSet<object> visitedMembers = new HashSet<object>();

        static ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<MemberInfo>> cache = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<MemberInfo>>();

        static ILog Log = LogManager.GetLogger<IEncryptionService>();
    }
}
