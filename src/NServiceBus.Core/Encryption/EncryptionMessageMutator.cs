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
                        throw new NotSupportedException("Cannot encrypt or decrypt indexed properties that return a WireEncryptedString.");
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
                return;

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
                return;

            if (EncryptionService == null)
                throw new InvalidOperationException(
                    String.Format("Cannot encrypt field {0} because no encryption service was configured.",
                                  member.Name));

            if (valueToEncrypt is WireEncryptedString)
            {
                var encryptedString = (WireEncryptedString)valueToEncrypt;
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
                return;

            if (EncryptionService == null)
                throw new InvalidOperationException(
                    String.Format("Cannot decrypt field {0} because no encryption service was configured.", property.Name));

            if (encryptedValue is WireEncryptedString)
            {
                Decrypt((WireEncryptedString)encryptedValue);
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
            if (encryptedValue.EncryptedValue == null)
                throw new InvalidOperationException("Encrypted property is missing encryption data");

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
        static IEnumerable<MemberInfo> GetFieldsAndProperties(object target)
        {
            if (target == null)
            {
                return new List<MemberInfo>();
            }

            var messageType = target.GetType();

            if (!cache.ContainsKey(messageType))
            {
                cache[messageType] = messageType.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m is FieldInfo || m is PropertyInfo)
                    .ToList();
            }

            return cache[messageType];
        }

        readonly HashSet<object> visitedMembers = new HashSet<object>();

        readonly static IDictionary<Type, IEnumerable<MemberInfo>> cache = new ConcurrentDictionary<Type, IEnumerable<MemberInfo>>();

        readonly static ILog Log = LogManager.GetLogger(typeof(IEncryptionService));
    }

    static class MemberInfoExtensions
    {
        static readonly IDictionary<PropertyInfo, LateBoundProperty> PropertyInfoToLateBoundProperty = new ConcurrentDictionary<PropertyInfo, LateBoundProperty>();
        static readonly IDictionary<FieldInfo, LateBoundField> FieldInfoToLateBoundField = new ConcurrentDictionary<FieldInfo, LateBoundField>();

        public static object GetValue(this MemberInfo member, object source)
        {
            if (member is FieldInfo)
            {
                var fieldInfo = member as FieldInfo;
                LateBoundField field;
                if (!FieldInfoToLateBoundField.TryGetValue(member as FieldInfo, out field))
                {
                    FieldInfoToLateBoundField[fieldInfo] = field = DelegateFactory.Create(fieldInfo);
                }

                return field.Invoke(source);
            }

            var propertyInfo = (PropertyInfo) member;
            
            if (!propertyInfo.CanRead)
            {
                if (propertyInfo.PropertyType.IsValueType)
                {
                    return Activator.CreateInstance(propertyInfo.PropertyType);
                }

                return null;
            }

            LateBoundProperty property;
            if (!PropertyInfoToLateBoundProperty.TryGetValue(propertyInfo, out property))
            {
                PropertyInfoToLateBoundProperty[propertyInfo] = property = DelegateFactory.Create(propertyInfo);
            }

            return property.Invoke(source);
        }

        static readonly IDictionary<PropertyInfo, LateBoundPropertySet> PropertyInfoToLateBoundPropertySet = new ConcurrentDictionary<PropertyInfo, LateBoundPropertySet>();
        static readonly IDictionary<FieldInfo, LateBoundFieldSet> FieldInfoToLateBoundFieldSet = new ConcurrentDictionary<FieldInfo, LateBoundFieldSet>();

        public static void SetValue(this MemberInfo member, object target, object value)
        {
            if (member is FieldInfo)
            {
                var fieldInfo = member as FieldInfo;
                LateBoundFieldSet fieldSet;
                if (!FieldInfoToLateBoundFieldSet.TryGetValue(fieldInfo, out fieldSet))
                {
                    FieldInfoToLateBoundFieldSet[fieldInfo] = fieldSet = DelegateFactory.CreateSet(fieldInfo);
                }

                fieldSet.Invoke(target, value);
            }
            else
            {
                var propertyInfo = member as PropertyInfo;
                LateBoundPropertySet propertySet;
                if (!PropertyInfoToLateBoundPropertySet.TryGetValue(propertyInfo, out propertySet))
                {
                    PropertyInfoToLateBoundPropertySet[propertyInfo] = propertySet = DelegateFactory.CreateSet(propertyInfo);
                }

                propertySet.Invoke(target, value);
            }
        }
    }
}
