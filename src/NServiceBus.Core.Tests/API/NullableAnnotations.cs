#if NET6_0_OR_GREATER
namespace NServiceBus.Core.Tests.API
{
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
        // Only available with net6.0+, implementation for net472 not worth it
        NullabilityInfoContext nullContext = new NullabilityInfoContext();

        [Test]
        public void ApproveNullableTypes()
        {
            var b = new StringBuilder()
                .AppendLine("The following types do not have annotations for nullable reference types.")
                .AppendLine("Changes that make this list longer should not be approved.")
                .AppendLine("-----");

            var context = new NullabilityInfoContext();

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
            var allInfo = AllMemberNullabilityInfoFor(type);
            var noNullInfoFor = allInfo.Where(info => info.WriteState is NullabilityState.Unknown || info.ReadState == NullabilityState.Unknown);
            return noNullInfoFor.Any();
        }

        IEnumerable<NullabilityInfo> AllMemberNullabilityInfoFor(Type type)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is PropertyInfo prop)
                {
                    yield return nullContext.Create(prop);
                }
                else if (member is FieldInfo field)
                {
                    yield return nullContext.Create(field);
                }
                else if (member is EventInfo evt)
                {
                    yield return nullContext.Create(evt);
                }
                else if (member is MethodBase method)
                {
                    var parameters = method.GetParameters();
                    foreach (var parameter in parameters)
                    {
                        yield return nullContext.Create(parameter);
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
}
#endif