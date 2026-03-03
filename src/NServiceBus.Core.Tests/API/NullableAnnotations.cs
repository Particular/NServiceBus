namespace NServiceBus.Core.Tests.API;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NServiceBus.Core.Tests.API.Infra;
using NUnit.Framework;
using Particular.Approvals;

[TestFixture]
public class NullableAnnotations
{
    readonly NullabilityInfoContext nullContext = new();

    [Test]
    public void ApproveNullableTypes()
    {
        var b = new StringBuilder()
            .AppendLine("The following types do not have annotations for nullable reference types.")
            .AppendLine("Changes that make this list longer should not be approved.")
            .AppendLine("-----");

        foreach (var type in NServiceBusAssembly.Types.Where(t => t.IsPublic).OrderBy(t => t.FullName))
        {
            if (HasNonAnnotatedMember(type))
            {
                b.AppendLine(type.FullName);
            }
        }

        Console.WriteLine(b.ToString());
        Approver.Verify(b.ToString());
    }

    bool HasNonAnnotatedMember(Type type)
    {
        var allInfo = AllMemberNullabilityInfoFor(type).ToArray();
        var noNullInfoFor = allInfo.Where(info =>
        {
            _ = type;

            if (info is { Item: ParameterInfo { ParameterType.IsGenericParameter: true } parameter, Info.ReadState: NullabilityState.Unknown })
            {
                var constraints = parameter.ParameterType.GetGenericParameterConstraints();
                // Any interface constraint is meaningful: it narrows T beyond "object".
                return !constraints.Any(c => c.IsInterface) &&
                       // Any base class constraint other than object is meaningful.
                       !constraints.Any(c => c.IsClass && c != typeof(object));
            }

            if (info.Info.ReadState == NullabilityState.Unknown)
            {
                return true;
            }

            if (info.Info.WriteState == NullabilityState.Unknown)
            {
                return info.Item is not PropertyInfo { CanWrite: false };
            }

            return false;
        }).ToArray();
        return noNullInfoFor.Length != 0;
    }

    IEnumerable<(object Item, NullabilityInfo Info)> AllMemberNullabilityInfoFor(Type type)
    {
        foreach (var member in type.GetMembers())
        {
            if (member.DeclaringType?.Assembly != typeof(IEndpointInstance).Assembly)
            {
                continue;
            }

            if (member.GetCustomAttribute<ObsoleteAttribute>() != null)
            {
                continue;
            }

            if (member is PropertyInfo prop)
            {
                if (!prop.PropertyType.IsValueType)
                {
                    yield return (prop, nullContext.Create(prop));
                }
            }
            else if (member is FieldInfo field)
            {
                if (!field.FieldType.IsValueType)
                {
                    yield return (field, nullContext.Create(field));
                }
            }
            else if (member is EventInfo evt)
            {
                yield return (evt, nullContext.Create(evt));
            }
            else if (member is MethodBase method)
            {
                var parameters = method.GetParameters();
                foreach (var parameter in parameters)
                {
                    yield return (parameter, nullContext.Create(parameter));
                }
            }
            else if (member.MemberType == MemberTypes.NestedType)
            {
                continue;
            }
            else
            {
                throw new Exception($"Unhandled MemberType: {member.MemberType}");
            }
        }
    }
}
