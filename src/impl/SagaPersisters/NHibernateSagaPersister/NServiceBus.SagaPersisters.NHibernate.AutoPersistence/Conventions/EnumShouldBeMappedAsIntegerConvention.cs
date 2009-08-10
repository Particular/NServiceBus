using System;
using FluentNHibernate.Conventions;
using FluentNHibernate.Mapping;

namespace NServiceBus.SagaPersisters.NHibernate.AutoPersistence.Conventions
{
    public class EnumShouldBeMappedAsIntegerConvention : IUserTypeConvention
    {
        public bool Accept(IProperty target)
        {
            return target.PropertyType.IsEnum;
        } 
        public void Apply(IProperty target)
        {
            target.CustomTypeIs(target.PropertyType);
        } 
        
        public bool Accept(Type type)
        {
            return type.IsEnum;
        }
    }

}