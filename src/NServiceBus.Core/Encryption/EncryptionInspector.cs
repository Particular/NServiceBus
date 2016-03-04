namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    class EncryptionInspector
    {
        Conventions conventions;

        public EncryptionInspector(Conventions conventions)
        {
            this.conventions = conventions;
        }

        static bool IsIndexedProperty(MemberInfo member)
        {
            var propertyInfo = member as PropertyInfo;

            return propertyInfo?.GetIndexParameters().Length > 0;
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

        public IEnumerable<Tuple<object, MemberInfo>> ScanObject(object root)
        {
            var visitedMembers = new HashSet<object>();
            return ScanObject(root, visitedMembers);
        }

        IEnumerable<Tuple<object, MemberInfo>> ScanObject(object root, ISet<object> visitedMembers)
        {
            if (root == null || visitedMembers.Contains(root))
            {
                yield break;
            }

            visitedMembers.Add(root);

            var members = GetFieldsAndProperties(root);

            foreach (var member in members)
            {
                if (IsEncryptedMember(member) && member.GetValue(root) != null)
                {
                    var value = member.GetValue(root);
                    if (value is string || value is WireEncryptedString)
                    {
                        yield return Tuple.Create(root, member);
                    }
                    else
                    {
                        throw new Exception("Only string properties are supported for convention based encryption. Check the configured conventions.");
                    }
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

                        foreach (var i in ScanObject(item, visitedMembers)) yield return i;
                    }
                }
                else
                {
                    foreach (var i in ScanObject(child, visitedMembers)) yield return i;
                }
            }
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

        static ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<MemberInfo>> cache = new ConcurrentDictionary<RuntimeTypeHandle, IEnumerable<MemberInfo>>();
    }
}
