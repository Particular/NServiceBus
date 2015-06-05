namespace NServiceBus.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using NServiceBus.Unicast;
    using NuDoq;
    using NUnit.Framework;
    using Enum = NuDoq.Enum;
    using Exception = System.Exception;

    [TestFixture]
    public class DocumentationTests
    {
        [Test]
        [Explicit]
        public void EnsureNoDocumentationIsEmpty()
        {
            var assembly = typeof(UnicastBus).Assembly;
            var codeBase = assembly.CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            var assemblyMembers = DocReader.Read(assembly, Path.ChangeExtension(path, "xml"));

            var list = GetListOfMissingDoco(assemblyMembers).ToList();

            if (list.Any())
            {
                var errors = string.Join("\r\n",list);
                throw new Exception("Some members have empty documentation:\r\n" + errors);
            }
        }

        static IEnumerable<string> GetListOfMissingDoco(AssemblyMembers assemblyMembers)
        {
            var visitor = new VerificationVisitor();
            visitor.VisitAssembly(assemblyMembers);
            return visitor.BadMembers
                .Distinct()
                .Select(FullMemberName);
        }

        static string FullMemberName(MemberInfo member)
        {
            if (member.ReflectedType != null)
            {
                var method = member as MethodBase;
                if (method != null)
                {
                    var methodInfo = method;
                    var parameters = string.Join(", ", methodInfo.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name));
                    return string.Format("{0}.{1}({2})", method.ReflectedType.FullName, method.Name, parameters);
                }
                return string.Format("{0}.{1}", member.ReflectedType.FullName, member.Name);
            }
            return member.Name;
        }

        public class VerificationVisitor : Visitor
        {
            Stack<MemberInfo> memberInfos = new Stack<MemberInfo>();

            public List<MemberInfo> BadMembers = new List<MemberInfo>();

            protected override void VisitElement(Element element)
            {
                AddIfEmpty(element);
                base.VisitElement(element);
            }

            void AddIfEmpty(Element element)
            {
                var text = element.ToText();
                if (text == null)
                {
                    return;
                }
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return;
                }
                var currentMember = memberInfos.Peek();
                if (currentMember == null)
                {
                    return;
                }
                var lineInfoField = GetLineInfoField(element.GetType());
                if (lineInfoField != null)
                {
                    var lineInfo = (XElement)lineInfoField.GetValue(element);

                    if (lineInfo != null)
                    {
                        var firstNode = lineInfo.FirstNode;
                        if (firstNode.ToString().Contains("inheritdoc"))
                        {
                            return;
                        }
                    }
                }
                BadMembers.Add(currentMember);
            }
            void ClearMember()
            {
                memberInfos.Pop();
            }
            void SetMember(MemberInfo memberInfo)
            {
                if (memberInfo == null)
                {
                    memberInfos.Push(null);
                    return;
                }

                var method = memberInfo as MethodBase;
                if (method != null)
                {
                    if (!method.DeclaringType.IsVisible)
                    {
                        memberInfos.Push(null);
                        return;
                    }
                }
                var constructorInfo = memberInfo as ConstructorInfo;
                if (constructorInfo != null)
                {
                    if (!constructorInfo.DeclaringType.IsVisible)
                    {
                        memberInfos.Push(null);
                        return;
                    }
                }
                var type = memberInfo as Type;
                if (type != null)
                {
                    if (!type.IsVisible)
                    {
                        memberInfos.Push(null);
                        return;
                    }
                }
                memberInfos.Push(memberInfo);
            }

            Dictionary<RuntimeTypeHandle, FieldInfo> fieldCache = new Dictionary<RuntimeTypeHandle, FieldInfo>();

            FieldInfo GetLineInfoField(Type type)
            {
                FieldInfo cachedField;
                var handle = type.TypeHandle;
                if (fieldCache.TryGetValue(handle, out cachedField))
                {
                    return cachedField;
                }
                var currentType = type;
                while (currentType != typeof(object))
                {
                    foreach (var field in currentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
                    {
                        if (field.Name == "lineInfo")
                        {
                            return fieldCache[handle]= field;
                        }
                    }
                    currentType = currentType.BaseType;
                }
                return fieldCache[handle] = null;
            }

            public override void VisitClass(Class type)
            {
                SetMember(type.Info);
                base.VisitClass(type);
                ClearMember();
            }


            public override void VisitEnum(Enum type)
            {
                SetMember(type.Info);
                base.VisitEnum(type);
                ClearMember();
            }

            public override void VisitEvent(Event @event)
            {
                SetMember(@event.Info);
                base.VisitEvent(@event);
                ClearMember();
            }

            public override void VisitStruct(Struct type)
            {
                SetMember(type.Info);
                base.VisitStruct(type);
                ClearMember();
            }


            public override void VisitInterface(Interface type)
            {
                SetMember(type.Info);
                base.VisitInterface(type);
                ClearMember();
            }

            public override void VisitField(Field field)
            {
                SetMember(field.Info);
                base.VisitField(field);
                ClearMember();
            }

            public override void VisitMethod(Method method)
            {
                SetMember(method.Info);
                base.VisitMethod(method);
                ClearMember();
            }

            public override void VisitProperty(Property property)
            {
                SetMember(property.Info);
                base.VisitProperty(property);
                ClearMember();
            }

        }
    }
}