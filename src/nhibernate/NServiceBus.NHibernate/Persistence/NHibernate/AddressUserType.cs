namespace NServiceBus.Persistence.NHibernate
{
    using System;
    using System.Data;
    using global::NHibernate;
    using global::NHibernate.SqlTypes;
    using global::NHibernate.UserTypes;

    public class AddressUserType : IUserType
    {
        private static readonly SqlType[] sqlTypes = new[] {NHibernateUtil.String.SqlType};

        public new bool Equals(object x, object y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }
            if (x == null || y == null)
            {
                return false;
            }

            return x.Equals(y);
        }

        public int GetHashCode(object x)
        {
            return x.GetHashCode();
        }

        public object NullSafeGet(IDataReader rs, string[] names, object owner)
        {
            var obj = NHibernateUtil.String.NullSafeGet(rs, names[0]) as string;

            return obj == null ? null : Address.Parse(obj);
        }

        public void NullSafeSet(IDbCommand cmd, object value, int index)
        {
            if (value == null)
            {
                ((IDataParameter) cmd.Parameters[index]).Value = DBNull.Value;
            }
            else
            {
                var address = (Address) value;
                ((IDataParameter) cmd.Parameters[index]).Value = address.ToString();
            }
        }

        public object DeepCopy(object value)
        {
            if (value == null)
            {
                return null;
            }

            return Address.Parse(value.ToString());
        }

        public object Replace(object original, object target, object owner)
        {
            return original;
        }

        public object Assemble(object cached, object owner)
        {
            return cached;
        }

        public object Disassemble(object value)
        {
            return value;
        }

        public SqlType[] SqlTypes
        {
            get { return sqlTypes; }
        }

        public Type ReturnedType
        {
            get { return typeof (Address); }
        }

        public bool IsMutable
        {
            get { return false; }
        }
    }
}