namespace NServiceBus.Core.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NuDoq;
using NUnit.Framework;
using Enum = NuDoq.Enum;
using Text = NuDoq.Text;

[TestFixture]
public class DocumentationTests
{
    [Test]
    public void EnsureNoDocumentationIsEmpty()
    {
        var assembly = typeof(Endpoint).Assembly;
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, Path.GetFileName(assembly.Location));
        var assemblyMembers = DocReader.Read(assembly, Path.ChangeExtension(path, "xml"));

        var list = GetListOfMissingDocs(assemblyMembers).ToList();

        if (list.Count == 0)
        {
            return;
        }

        var errors = string.Join(Environment.NewLine, list);
        Assert.Fail($"Some members have empty documentation or have a sentence that does not end with a period:{Environment.NewLine}{errors}");
    }

    static IEnumerable<string> GetListOfMissingDocs(AssemblyMembers assemblyMembers)
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
                return $"{method.ReflectedType.FullName}.{method.Name}({parameters})";
            }
            return $"{member.ReflectedType.FullName}.{member.Name}";
        }
        return member.Name;
    }

    public class VerificationVisitor : Visitor
    {
        readonly Stack<MemberInfo> memberInfos = new();

        public List<MemberInfo> BadMembers = [];

        protected override void VisitElement(Element element)
        {
            AddIfEmpty(element);
            base.VisitElement(element);
        }

        void AddIfEmpty(Element element)
        {
            if (SkipElementTypes(element))
            {
                return;
            }

            var text = element.ToText();
            if (text == null)
            {
                return;
            }
            if (!string.IsNullOrWhiteSpace(text))
            {
                if (text.Trim().EndsWith('.'))
                {
                    return;
                }
            }
            if (memberInfos.Count == 0)
            {
                return;
            }
            var currentMember = memberInfos.Peek();
            if (currentMember == null)
            {
                return;
            }
            if (IsInheritDoc(element))
            {
                return;
            }
            var declaringType = currentMember.DeclaringType;
            if (declaringType != null && declaringType.FullName.Contains("FastExpressionCompiler"))
            {
                return;
            }
            BadMembers.Add(currentMember);
        }

        static bool SkipElementTypes(Element element) =>
            element switch
            {
                TypeParamRef or C or SeeAlso or UnknownElement or Code or See or Text or ParamRef or ExtensionMethod => true,
                _ => false,
            };

        bool IsInheritDoc(Element element)
        {
            var lineInfoField = GetLineInfoField(element.GetType());
            var lineInfo = (XElement)lineInfoField?.GetValue(element);

            if (lineInfo != null)
            {
                var firstNode = lineInfo.FirstNode;
                if (firstNode.ToString().Contains("inheritdoc"))
                {
                    return true;
                }
            }
            return false;
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

        Dictionary<RuntimeTypeHandle, FieldInfo> fieldCache = [];

        FieldInfo GetLineInfoField(Type type)
        {
            var handle = type.TypeHandle;
            if (fieldCache.TryGetValue(handle, out var cachedField))
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
                        return fieldCache[handle] = field;
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